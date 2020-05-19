using System;
using System.Threading;
using System.Threading.Tasks;
using Indexer.Common.Messaging.InMemoryBus;
using Microsoft.Extensions.Logging;

namespace Indexer.Common.Domain.Indexing
{
    public sealed class FirstPassHistoryIndexingJob
    {
        private readonly ILogger<FirstPassHistoryIndexingJob> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly FirstPassHistoryIndexerId _indexerId;
        private readonly long _stopBlock;
        private readonly TimeSpan _delayOnBlockNotFound;
        private readonly IFirstPassHistoryIndexersRepository _indexersRepository;
        private readonly IBlocksReader _blocksReader;
        private readonly IBlocksRepository _blocksRepository;
        private readonly IInMemoryBus _inMemoryBus;
        private readonly Timer _timer;
        private readonly ManualResetEventSlim _done;
        private readonly CancellationTokenSource _cts;
        
        public FirstPassHistoryIndexingJob(ILogger<FirstPassHistoryIndexingJob> logger,
            ILoggerFactory loggerFactory,
            FirstPassHistoryIndexerId indexerId,
            long stopBlock,
            TimeSpan delayOnBlockNotFound,
            IFirstPassHistoryIndexersRepository indexersRepository,
            IBlocksReader blocksReader,
            IBlocksRepository blocksRepository,
            IInMemoryBus inMemoryBus)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
            _indexerId = indexerId;
            _stopBlock = stopBlock;
            _delayOnBlockNotFound = delayOnBlockNotFound;
            _indexersRepository = indexersRepository;
            _blocksReader = blocksReader;
            _blocksRepository = blocksRepository;
            _inMemoryBus = inMemoryBus;

            _timer = new Timer(TimerCallback, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _done = new ManualResetEventSlim(false);
            _cts = new CancellationTokenSource();

            _logger.LogInformation("First-pass history indexing job is being created {@context}", new
            {
                BlockchainId = _indexerId.BlockchainId,
                StartBlock = _indexerId.StartBlock,
                StopBlock = _stopBlock,
                DelayOnBlockNotFound = _delayOnBlockNotFound
            });
        }

        public void Start()
        {
            _logger.LogInformation("First-pass history indexing job is being started {@context}", new
            {
                BlockchainId = _indexerId.BlockchainId,
                StartBlock = _indexerId.StartBlock
            });

            _timer.Change(TimeSpan.Zero, Timeout.InfiniteTimeSpan);
        }

        public void Stop()
        {
            if (_cts.IsCancellationRequested)
            {
                return;
            }

            _logger.LogInformation("First-pass history indexing job is being stopped {@context}", new
            {
                BlockchainId = _indexerId.BlockchainId,
                StartBlock = _indexerId.StartBlock
            });

            _cts.Cancel();
        }

        public void Wait()
        {
            _done.Wait();

            _logger.LogInformation("First-pass history indexing job has been stopped {@context}", new
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
                _logger.LogError(ex, "Error while executing first-pass history indexing job {@context}", new
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
            var batchInitialBlock = indexer.NextBlock;

            while (!_cts.IsCancellationRequested)
            {
                var indexingResult = await indexer.IndexNextBlock(
                    _loggerFactory.CreateLogger<FirstPassHistoryIndexer>(),
                    _blocksReader,
                    _blocksRepository,
                    _inMemoryBus);

                if (indexingResult == FirstPassHistoryIndexingResult.IndexingCompleted)
                {
                    _logger.LogInformation("First-pass history indexing job is completed {@context}",
                        new
                        {
                            BlockchainId = _indexerId.BlockchainId,
                            StartBlock = _indexerId.StartBlock,
                            StopBlock = _stopBlock
                        });
                    
                    if (indexer.NextBlock != batchInitialBlock)
                    {
                        await _indexersRepository.Update(indexer);
                    }

                    Stop();

                    break;
                }

                // Saves the indexer state only every 100 blocks

                // TODO: Move batch size to the config
                if (indexer.NextBlock - batchInitialBlock >= 100)
                {
                    // TODO: Update indexer Version or re-read it from DB
                    await _indexersRepository.Update(indexer);

                    batchInitialBlock = indexer.NextBlock;
                }
            }
        }
    }
}
