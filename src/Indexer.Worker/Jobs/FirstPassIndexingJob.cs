using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Indexer.Common.Domain.Blocks;
using Indexer.Common.Domain.Indexing.FirstPass;
using Indexer.Common.Domain.Transactions.Transfers;
using Indexer.Common.Messaging.InMemoryBus;
using Indexer.Common.Persistence.Entities.BlockHeaders;
using Indexer.Common.Persistence.Entities.FirstPassIndexers;
using Indexer.Common.Persistence.Entities.InputCoins;
using Indexer.Common.Persistence.Entities.TransactionHeaders;
using Indexer.Common.Persistence.Entities.UnspentCoins;
using Indexer.Common.Telemetry;
using Microsoft.Extensions.Logging;

namespace Indexer.Worker.Jobs
{
    internal sealed class FirstPassIndexingJob : IDisposable
    {
        private readonly ILogger<FirstPassIndexingJob> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly FirstPassIndexerId _indexerId;
        private readonly long _stopBlock;
        private readonly IFirstPassIndexersRepository _indexersRepository;
        private readonly IBlocksReader _blocksReader;
        private readonly UnspentCoinsFactory _unspentCoinsFactory;
        private readonly IBlockHeadersRepository _blockHeadersRepository;
        private readonly ITransactionHeadersRepository _transactionHeadersRepository;
        private readonly IInputCoinsRepository _inputCoinsRepository;
        private readonly IUnspentCoinsRepository _unspentCoinsRepository;
        private readonly IInMemoryBus _inMemoryBus;
        private readonly SecondPassIndexingJobsManager _secondPassIndexingJobsManager;
        private readonly IAppInsight _appInsight;
        private readonly BackgroundJob _job;
        private FirstPassIndexer _indexer;

        public FirstPassIndexingJob(ILogger<FirstPassIndexingJob> logger,
            ILoggerFactory loggerFactory,
            FirstPassIndexerId indexerId,
            long stopBlock,
            IFirstPassIndexersRepository indexersRepository,
            IBlocksReader blocksReader,
            UnspentCoinsFactory unspentCoinsFactory,
            IBlockHeadersRepository blockHeadersRepository,
            ITransactionHeadersRepository transactionHeadersRepository,
            IInputCoinsRepository inputCoinsRepository,
            IUnspentCoinsRepository unspentCoinsRepository,
            IInMemoryBus inMemoryBus,
            SecondPassIndexingJobsManager secondPassIndexingJobsManager,
            IAppInsight appInsight)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
            _indexerId = indexerId;
            _stopBlock = stopBlock;
            _indexersRepository = indexersRepository;
            _blocksReader = blocksReader;
            _unspentCoinsFactory = unspentCoinsFactory;
            _blockHeadersRepository = blockHeadersRepository;
            _transactionHeadersRepository = transactionHeadersRepository;
            _inputCoinsRepository = inputCoinsRepository;
            _unspentCoinsRepository = unspentCoinsRepository;
            _inMemoryBus = inMemoryBus;
            _secondPassIndexingJobsManager = secondPassIndexingJobsManager;
            _appInsight = appInsight;

            _job = new BackgroundJob(
                _logger,
                "First-pass indexing",
                () => new
                {
                    BlockchainId = _indexerId.BlockchainId,
                    StartBlock = _indexerId.StartBlock,
                    StopBlock = _stopBlock,
                    NextBlock = _indexer?.NextBlock
                },
                IndexBlocksBatch);
        }

        public async Task Start()
        {
            _indexer = await _indexersRepository.Get(_indexerId);

            _job.Start();
        }

        public void Stop()
        {
            _job.Stop();
        }

        public Task Wait()
        {
            return _job.Wait();
        }

        public void Dispose()
        {
            _job.Dispose();
        }

        private async Task IndexBlocksBatch()
        {
            try
            {
                var batchInitialBlock = _indexer.NextBlock;

                // TODO: Move batch size to the config

                while (!_job.IsCancellationRequested &&
                       _indexer.NextBlock - batchInitialBlock < 100)
                {
                    // TODO: Add some delay in case of an error to reduce workload on the integration and DB

                    var indexingResult = await IndexNextBlock();

                    if (indexingResult == FirstPassIndexingResult.IndexingCompleted)
                    {
                        _logger.LogInformation("First-pass indexing job is completed {@context}",
                            new
                            {
                                BlockchainId = _indexerId.BlockchainId,
                                StartBlock = _indexerId.StartBlock,
                                StopBlock = _stopBlock
                            });

                        _indexer = await _indexersRepository.Update(_indexer);

                        await StartSecondPassIndexerJobIfFirstPassDone();

                        Stop();

                        return;
                    }
                }

                // Saves the indexer state only in the end of the batch
                _indexer = await _indexersRepository.Update(_indexer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute first-pass indexing job {@context}",  new
                {
                    BlockchainId = _indexerId.BlockchainId,
                    StartBlock = _indexerId.StartBlock,
                    StopBlock = _stopBlock,
                    NextBlock = _indexer.NextBlock
                });

                _indexer = await _indexersRepository.Get(_indexerId);
            }
        }

        private async Task<FirstPassIndexingResult> IndexNextBlock()
        {
            var telemetry = _appInsight.StartRequest("First-pass block indexing",
                new Dictionary<string, string>
                {
                    ["blockchainId"] = _indexer.BlockchainId,
                    ["startBlock"] = _indexer.StartBlock.ToString(),
                    ["nextBlock"] = _indexer.NextBlock.ToString()
                });

            try
            {
                var result = await _indexer.IndexNextBlock(
                    _loggerFactory.CreateLogger<FirstPassIndexer>(),
                    _blocksReader,
                    _unspentCoinsFactory,
                    _blockHeadersRepository,
                    _transactionHeadersRepository,
                    _inputCoinsRepository, 
                    _unspentCoinsRepository,
                    _inMemoryBus);

                telemetry.ResponseCode = result.ToString();

                return result;
            }
            catch (Exception ex)
            {
                telemetry.Fail(ex);

                throw;
            }
            finally
            {
                telemetry.Stop();
            }
        }

        private async Task StartSecondPassIndexerJobIfFirstPassDone()
        {
            var indexers = await _indexersRepository.GetByBlockchain(_indexerId.BlockchainId);

            if (indexers.All(x => x.IsCompleted))
            {
                await _secondPassIndexingJobsManager.EnsureStarted(_indexerId.BlockchainId);
            }
        }
    }
}
