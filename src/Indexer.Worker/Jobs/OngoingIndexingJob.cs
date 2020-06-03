using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Indexer.Common.Domain.Indexing;
using Indexer.Common.Domain.Transactions;
using Indexer.Common.Telemetry;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Indexer.Worker.Jobs
{
    internal sealed class OngoingIndexingJob : IDisposable
    {
        private readonly ILogger<OngoingIndexingJob> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly string _blockchainId;
        private readonly TimeSpan _delayOnBlockNotFound;
        private readonly IOngoingIndexersRepository _indexersRepository;
        private readonly ITransactionHeadersRepository _transactionHeadersRepository;
        private readonly IBlocksReader _blocksReader;
        private readonly ChainWalker _chainWalker;
        private readonly IPublishEndpoint _publisher;
        private readonly IAppInsight _appInsight;
        private readonly Timer _timer;
        private readonly ManualResetEventSlim _done;
        private readonly CancellationTokenSource _cts;
        private OngoingIndexer _indexer;

        public OngoingIndexingJob(ILogger<OngoingIndexingJob> logger,
            ILoggerFactory loggerFactory,
            string blockchainId,
            TimeSpan delayOnBlockNotFound,
            IOngoingIndexersRepository indexersRepository,
            ITransactionHeadersRepository transactionHeadersRepository,
            IBlocksReader blocksReader,
            ChainWalker chainWalker,
            IPublishEndpoint publisher,
            IAppInsight appInsight)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
            _blockchainId = blockchainId;
            _delayOnBlockNotFound = delayOnBlockNotFound;
            _indexersRepository = indexersRepository;
            _transactionHeadersRepository = transactionHeadersRepository;
            _blocksReader = blocksReader;
            _chainWalker = chainWalker;
            _publisher = publisher;
            _appInsight = appInsight;

            _timer = new Timer(TimerCallback, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _done = new ManualResetEventSlim(false);
            _cts = new CancellationTokenSource();

            _logger.LogInformation("Ongoing indexing job is being created {@context}", new
            {
                BlockchainId = _blockchainId,
                DelayOnBlockNotFound = _delayOnBlockNotFound
            });
        }

        public async Task Start()
        {
            _indexer = await _indexersRepository.Get(_blockchainId);

            _logger.LogInformation("Ongoing indexing job is being started {@context}", new
            {
                BlockchainId = _blockchainId
            });

            _timer.Change(TimeSpan.Zero, Timeout.InfiniteTimeSpan);
        }

        public void Stop()
        {
            if (_cts.IsCancellationRequested)
            {
                return;
            }

            _logger.LogInformation("Ongoing indexing job is being stopped {@context}", new
            {
                BlockchainId = _blockchainId
            });

            _cts.Cancel();
        }

        public void Wait()
        {
            _done.Wait();

            _logger.LogInformation("Ongoing indexing job has been stopped {@context}", new
            {
                BlockchainId = _blockchainId
            });
        }

        public void Dispose()
        {
            _timer.Dispose();
            _cts.Dispose();
            _done.Dispose();
        }

        private void TimerCallback(object state)
        {
            try
            {
                IndexAvailableBlocks().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while executing ongoing indexing job {@context}", new
                {
                    BlockchainId = _blockchainId
                });
            }
            finally
            {
                if (!_cts.IsCancellationRequested)
                {
                    _timer.Change(_delayOnBlockNotFound, Timeout.InfiniteTimeSpan);
                }
            }

            if (_cts.IsCancellationRequested)
            {
                _done.Set();
            }
        }

        private async Task IndexAvailableBlocks()
        {
            try
            {
                var batchInitialBlock = _indexer.NextBlock;
                var batchBackgroundTasks = new List<Task>();

                while (!_cts.IsCancellationRequested)
                {
                    var telemetry = _appInsight.StartRequest("Ongoing block indexing",
                        new Dictionary<string, string>
                        {
                            ["job"] = "Ongoing indexing",
                            ["blockchainId"] = _indexer.BlockchainId,
                            ["nextBlock"] = _indexer.NextBlock.ToString()
                        });

                    try
                    {
                        // TODO: Add some delay in case of an error to reduce workload on the integration and DB

                        var indexingResult = await _indexer.IndexNextBlock(
                            _loggerFactory.CreateLogger<OngoingIndexer>(),
                            _blocksReader,
                            _chainWalker,
                            _transactionHeadersRepository,
                            _publisher);

                        batchBackgroundTasks.AddRange(indexingResult.BackgroundTasks);

                        telemetry.ResponseCode = indexingResult.BlockResult.ToString();

                        if (indexingResult.BlockResult == OngoingBlockIndexingResult.BlockNotFound)
                        {
                            _logger.LogDebug("Ongoing block is not found {@context}",
                                new
                                {
                                    BlockchainId = _indexer.BlockchainId,
                                    NextBlock = _indexer.NextBlock
                                });

                            break;
                        }

                        // Saves the indexer state only every 100 blocks

                        // TODO: Move batch size to the config

                        if (_indexer.NextBlock - batchInitialBlock >= 100)
                        {
                            // This is needed to mitigate single IO operation latency
                            await Task.WhenAll(batchBackgroundTasks);

                            _indexer = await _indexersRepository.Update(_indexer);

                            batchInitialBlock = _indexer.NextBlock;
                        }
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

                if (_indexer.NextBlock != batchInitialBlock)
                {
                    // This is needed to mitigate single IO operation latency
                    await Task.WhenAll(batchBackgroundTasks);

                    _indexer = await _indexersRepository.Update(_indexer);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute ongoing indexing job");

                _indexer = await _indexersRepository.Get(_blockchainId);
            }
        }
    }
}
