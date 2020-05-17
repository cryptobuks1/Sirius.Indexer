using System;
using System.Threading;
using System.Threading.Tasks;
using Indexer.Common.InMemoryBus;
using Microsoft.Extensions.Logging;

namespace Indexer.Common.Domain.Indexing
{
    public sealed class FirstPassIndexingJob
    {
        private readonly ILogger<FirstPassIndexingJob> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly FirstPassIndexerId _indexerId;
        private readonly long? _stopBlock;
        private readonly TimeSpan _delayOnBlockNotFound;
        private readonly IFirstPassIndexersRepository _indexersRepository;
        private readonly IBlocksReader _blocksReader;
        private readonly BlocksProcessor _blocksProcessor;
        private readonly IInMemoryBus _inMemoryBus;
        private readonly Timer _timer;
        private readonly ManualResetEventSlim _done;
        private readonly CancellationTokenSource _cts;
        
        public FirstPassIndexingJob(ILogger<FirstPassIndexingJob> logger,
            ILoggerFactory loggerFactory,
            FirstPassIndexerId indexerId,
            long? stopBlock,
            TimeSpan delayOnBlockNotFound,
            IFirstPassIndexersRepository indexersRepository,
            IBlocksReader blocksReader,
            BlocksProcessor blocksProcessor,
            IInMemoryBus inMemoryBus)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
            _indexerId = indexerId;
            _stopBlock = stopBlock;
            _delayOnBlockNotFound = delayOnBlockNotFound;
            _indexersRepository = indexersRepository;
            _blocksReader = blocksReader;
            _blocksProcessor = blocksProcessor;
            _inMemoryBus = inMemoryBus;

            _timer = new Timer(TimerCallback, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _done = new ManualResetEventSlim(false);
            _cts = new CancellationTokenSource();

            _logger.LogInformation("First-pass indexing job is being created {@context}", new
            {
                BlockchainId = _indexerId.BlockchainId,
                StartBlock = _indexerId.StartBlock,
                StopBlock = _stopBlock,
                DelayOnBlockNotFound = _delayOnBlockNotFound
            });
        }

        public async Task Start()
        {
            _logger.LogInformation("First-pass indexing job is being started {@context}", new
            {
                BlockchainId = _indexerId.BlockchainId,
                StartBlock = _indexerId.StartBlock
            });

            if (await ProvisionIndexer())
            {
                _timer.Change(TimeSpan.Zero, Timeout.InfiniteTimeSpan);
            }
        }

        public void Stop()
        {
            if (_cts.IsCancellationRequested)
            {
                return;
            }

            _logger.LogInformation("First-pass indexing job is being stopped {@context}", new
            {
                BlockchainId = _indexerId.BlockchainId,
                StartBlock = _indexerId.StartBlock
            });

            _cts.Cancel();
        }

        public void Wait()
        {
            _done.Wait();

            _logger.LogInformation("First-pass indexing job has been stopped {@context}", new
            {
                BlockchainId = _indexerId.BlockchainId,
                StartBlock = _indexerId.StartBlock
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
                _logger.LogError(ex, "Error while executing first-pass indexing job {@context}", new
                {
                    BlockchainId = _indexerId.BlockchainId,
                    StartBlock = _indexerId.StartBlock
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
            var indexer = await _indexersRepository.Get(_indexerId);
            var batchInitialSequence = indexer.Sequence;

            async Task InterruptBatch()
            {
                await PublishIndexerEvents(indexer);

                // Saves the indexer if it was changed since the last batch

                if (batchInitialSequence != indexer.Sequence)
                {
                    await _indexersRepository.Update(indexer);
                }
            }
            
            while (!_cts.IsCancellationRequested)
            {
                var indexingResult = await indexer.IndexNextBlock(
                    _loggerFactory.CreateLogger<FirstPassIndexer>(),
                    _blocksReader,
                    _blocksProcessor);

                if (indexingResult == BlockIndexingResult.BlockNotFound)
                {
                    await InterruptBatch();

                    break;
                }

                if (indexingResult == BlockIndexingResult.ThreadCompleted)
                {
                    _logger.LogInformation("First-pass indexing job is completed {@context}",
                        new
                        {
                            BlockchainId = _indexerId.BlockchainId,
                            StartBlock = _indexerId.StartBlock,
                            StopBlock = _stopBlock
                        });

                    await InterruptBatch();

                    Stop();

                    break;
                }

                await PublishIndexerEvents(indexer);
                indexer.ClearEvents();

                // Saves the indexer state only every 100 blocks if there are a lot of blocks in a row

                if (indexer.Sequence - batchInitialSequence >= 100)
                {
                    // TODO: Update indexer Version or re-read it from DB
                    await _indexersRepository.Update(indexer);

                    batchInitialSequence = indexer.Sequence;
                }
            }
        }

        private async Task PublishIndexerEvents(FirstPassIndexer indexer)
        {
            foreach (var evt in indexer.Events)
            {
                await _inMemoryBus.Publish(evt);
            }
        }

        private async Task<bool> ProvisionIndexer()
        {
            var indexer = await _indexersRepository.GetOrDefault(_indexerId);

            if (indexer == null)
            {
                _logger.LogInformation("First-pass indexer is being created {@context}", new
                {
                    BlockchainId = _indexerId.BlockchainId,
                    StartBlock = _indexerId.StartBlock,
                    StopBlock = _stopBlock
                });

                indexer = _stopBlock.HasValue
                    ? FirstPassIndexer.StartIrreversible(_indexerId, _stopBlock.Value)
                    : FirstPassIndexer.StartReversible(_indexerId);

                await _indexersRepository.Add(indexer);
            }
            else
            {
                _logger.LogInformation("First-pass indexer has been found {@context}", new
                {
                    BlockchainId = _indexerId.BlockchainId,
                    StartBlock = _indexerId.StartBlock,
                    StopBlock = _stopBlock,
                    NextBlock = indexer.NextBlock
                });

                if (indexer.IsCompleted)
                {
                    _logger.LogInformation("First-pass indexer is completed {@context}", new
                    {
                        BlockchainId = _indexerId.BlockchainId,
                        StartBlock = _indexerId.StartBlock,
                        StopBlock = _stopBlock,
                        NextBlock = indexer.NextBlock
                    });

                    Stop();

                    return false;
                }
            }

            return true;
        }
    }
}
