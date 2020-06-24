﻿using System;
using System.Threading.Tasks;
using Indexer.Common.Domain.Indexing.Common.CoinBlocks;
using Indexer.Common.Domain.Indexing.FirstPass;
using Indexer.Common.Domain.Indexing.SecondPass;
using Indexer.Common.Persistence.Entities.BlockHeaders;
using Indexer.Common.Persistence.Entities.SecondPassIndexers;
using Indexer.Common.Telemetry;
using Indexer.Worker.Jobs;
using Indexer.Worker.Limiters;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Indexer.Worker.MessageConsumers
{
    internal class FirstPassBlockDetectedConsumer : IConsumer<FirstPassBlockDetected>
    {
        private static readonly RateLimiter RateLimiter = new RateLimiter(1, TimeSpan.FromSeconds(1));
        private static readonly ConcurrencyLimiter ConcurrencyLimiter = new ConcurrencyLimiter(1);

        private readonly ILoggerFactory _loggerFactory;
        private readonly IBlockHeadersRepository _blockHeadersRepository;
        private readonly ISecondPassIndexersRepository _secondPassIndexersRepository;
        private readonly SecondPassIndexingJobsManager _secondPassIndexingJobsManager;
        private readonly OngoingIndexingJobsManager _ongoingIndexingJobsManager;
        private readonly IAppInsight _appInsight;
        private readonly CoinsSecondaryBlockProcessor _coinsSecondaryBlockProcessor;

        public FirstPassBlockDetectedConsumer(ILoggerFactory loggerFactory,
            IBlockHeadersRepository blockHeadersRepository,
            ISecondPassIndexersRepository secondPassIndexersRepository,
            SecondPassIndexingJobsManager secondPassIndexingJobsManager,
            OngoingIndexingJobsManager ongoingIndexingJobsManager,
            IAppInsight appInsight,
            CoinsSecondaryBlockProcessor coinsSecondaryBlockProcessor)
        {
            _loggerFactory = loggerFactory;
            _blockHeadersRepository = blockHeadersRepository;
            _secondPassIndexersRepository = secondPassIndexersRepository;
            _secondPassIndexingJobsManager = secondPassIndexingJobsManager;
            _ongoingIndexingJobsManager = ongoingIndexingJobsManager;
            _appInsight = appInsight;
            _coinsSecondaryBlockProcessor = coinsSecondaryBlockProcessor;
        }

        public async Task Consume(ConsumeContext<FirstPassBlockDetected> context)
        {
            var evt = context.Message;

            if (!await RateLimiter.Wait($"{nameof(FirstPassBlockDetected)}-{evt.BlockchainId}"))
            {
                // Just skips rate-limited event
                return;
            }

            using var concurrencyLimiter = await ConcurrencyLimiter.Enter($"{nameof(FirstPassBlockDetected)}-{evt.BlockchainId}");

            _secondPassIndexingJobsManager.BlockStart(evt.BlockchainId);

            SecondPassIndexingResult secondPassIndexingResult;

            try
            {
                if (_secondPassIndexingJobsManager.IsStarted(evt.BlockchainId))
                {
                    return;
                }

                var secondPassIndexer = await _secondPassIndexersRepository.Get(evt.BlockchainId);

                secondPassIndexingResult = await secondPassIndexer.IndexAvailableBlocks(
                    _loggerFactory.CreateLogger<SecondPassIndexer>(),
                    // TODO: To config
                    100,
                    _blockHeadersRepository,
                    _appInsight,
                    _coinsSecondaryBlockProcessor);

                await _secondPassIndexersRepository.Update(secondPassIndexer);
            }
            finally
            {
                _secondPassIndexingJobsManager.AllowStart(evt.BlockchainId);
            }

            if (secondPassIndexingResult == SecondPassIndexingResult.IndexingCompleted)
            {
                await _ongoingIndexingJobsManager.EnsureStarted(evt.BlockchainId);
            }
        }
    }
}
