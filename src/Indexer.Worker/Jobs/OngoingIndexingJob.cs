using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Indexer.Common.Domain.Indexing.Ongoing;
using Indexer.Common.Domain.Indexing.Ongoing.BlockCancelling;
using Indexer.Common.Domain.Indexing.Ongoing.BlockIndexing;
using Indexer.Common.Persistence.Entities.Blockchains;
using Indexer.Common.Persistence.Entities.OngoingIndexers;
using Indexer.Common.Telemetry;
using Microsoft.Extensions.Logging;
using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Worker.Jobs
{
    internal sealed class OngoingIndexingJob : IDisposable
    {
        private readonly ILogger<OngoingIndexingJob> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly string _blockchainId;
        private readonly DoubleSpendingProtectionType _blockchainDoubleSpendingProtectionType;
        private readonly TimeSpan _delayOnBlockNotFound;
        private readonly IBlockchainSchemaBuilder _blockchainSchemaBuilder;
        private readonly IOngoingIndexersRepository _indexersRepository;
        private readonly ChainWalker _chainWalker;
        private readonly IAppInsight _appInsight;
        private readonly Timer _timer;
        private readonly ManualResetEventSlim _done;
        private readonly CancellationTokenSource _cts;
        private OngoingIndexer _indexer;
        private readonly OngoingIndexingStrategyFactory _ongoingIndexingStrategyFactory;
        private readonly BlockCancelerFactory _blockCancelerFactory;
        
        public OngoingIndexingJob(ILogger<OngoingIndexingJob> logger,
            ILoggerFactory loggerFactory, 
            string blockchainId,
            DoubleSpendingProtectionType blockchainDoubleSpendingProtectionType,
            TimeSpan delayOnBlockNotFound,
            IBlockchainSchemaBuilder blockchainSchemaBuilder,
            IOngoingIndexersRepository indexersRepository,
            ChainWalker chainWalker,
            IAppInsight appInsight,
            OngoingIndexingStrategyFactory ongoingIndexingStrategyFactory,
            BlockCancelerFactory blockCancelerFactory)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
            _blockchainId = blockchainId;
            _blockchainDoubleSpendingProtectionType = blockchainDoubleSpendingProtectionType;
            _delayOnBlockNotFound = delayOnBlockNotFound;
            _blockchainSchemaBuilder = blockchainSchemaBuilder;
            _indexersRepository = indexersRepository;
            _chainWalker = chainWalker;
            _appInsight = appInsight;
            _ongoingIndexingStrategyFactory = ongoingIndexingStrategyFactory;
            _blockCancelerFactory = blockCancelerFactory;

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
                BlockchainId = _blockchainId,
                NextBlock = _indexer.NextBlock
            });

            if (_indexer.NextBlock == _indexer.StartBlock)
            {
                await _blockchainSchemaBuilder.UpgradeToOngoingIndexing(_blockchainId, _blockchainDoubleSpendingProtectionType);
            }
            else
            {
                _logger.LogInformation("Blockchain {@blockchainId} DB schema already proceeded to ongoing indexing", _blockchainId);
            }

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
                BlockchainId = _blockchainId,
                NextBlock = _indexer?.NextBlock
            });

            _cts.Cancel();
        }

        public void Wait()
        {
            _done.Wait();

            _logger.LogInformation("Ongoing indexing job has been stopped {@context}", new
            {
                BlockchainId = _blockchainId,
                NextBlock = _indexer?.NextBlock
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
                    BlockchainId = _blockchainId,
                    NextBlock = _indexer?.NextBlock
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
                        var indexingResult = await _indexer.IndexNextBlock(
                            _loggerFactory.CreateLogger<OngoingIndexer>(),
                            _chainWalker,
                            _ongoingIndexingStrategyFactory, 
                            _blockCancelerFactory);

                        telemetry.ResponseCode = indexingResult.ToString();

                        if (indexingResult == OngoingBlockIndexingResult.BlockNotFound)
                        {
                            _logger.LogDebug("Ongoing block is not found {@context}",
                                new
                                {
                                    BlockchainId = _indexer.BlockchainId,
                                    NextBlock = _indexer.NextBlock
                                });

                            break;
                        }

                        // Saves the indexer state only every N blocks

                        // TODO: Move batch size to the config

                        if (_indexer.NextBlock - batchInitialBlock >= 100)
                        {
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
                    _indexer = await _indexersRepository.Update(_indexer);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute ongoing indexing job {@context}", new
                {
                    BlockchainId = _indexer.BlockchainId,
                    NextBlock = _indexer.NextBlock
                });

                _indexer = await _indexersRepository.Get(_blockchainId);

                await Task.Delay(TimeSpan.FromSeconds(30));
            }
        }
    }
}
