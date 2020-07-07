﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Domain.Indexing.FirstPass;
using Indexer.Common.Persistence.Entities.FirstPassIndexers;
using Indexer.Common.Telemetry;
using Microsoft.Extensions.Logging;

namespace Indexer.Worker.Jobs
{
    internal sealed class FirstPassIndexingJob : IDisposable
    {
        private readonly ILogger<FirstPassIndexingJob> _logger;
        private readonly FirstPassIndexerId _indexerId;
        private readonly long _stopBlock;
        private readonly IFirstPassIndexersRepository _indexersRepository;
        private readonly IAppInsight _appInsight;
        private readonly BackgroundJob _job;
        private FirstPassIndexer _indexer;
        private readonly FirstPassIndexingStrategyFactory _firstPassIndexingStrategyFactory;
        
        public FirstPassIndexingJob(ILogger<FirstPassIndexingJob> logger,
            FirstPassIndexerId indexerId,
            long stopBlock,
            IFirstPassIndexersRepository indexersRepository,
            IAppInsight appInsight,
            FirstPassIndexingStrategyFactory firstPassIndexingStrategyFactory)
        {
            _logger = logger;
            _indexerId = indexerId;
            _stopBlock = stopBlock;
            _indexersRepository = indexersRepository;
            _appInsight = appInsight;
            _firstPassIndexingStrategyFactory = firstPassIndexingStrategyFactory;

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

                await Task.Delay(TimeSpan.FromSeconds(30));
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
                var result = await _indexer.IndexNextBlock(_firstPassIndexingStrategyFactory);

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
    }
}
