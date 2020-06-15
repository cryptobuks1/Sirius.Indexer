using System;
using System.Threading.Tasks;
using Indexer.Common.Domain.Indexing.FirstPass;
using Indexer.Common.Domain.Indexing.SecondPass;
using Indexer.Common.Persistence.Entities.BalanceUpdates;
using Indexer.Common.Persistence.Entities.BlockHeaders;
using Indexer.Common.Persistence.Entities.Fees;
using Indexer.Common.Persistence.Entities.InputCoins;
using Indexer.Common.Persistence.Entities.SecondPassIndexers;
using Indexer.Common.Persistence.Entities.SpentCoins;
using Indexer.Common.Persistence.Entities.UnspentCoins;
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
        private readonly OngoingIndexingJobsManager _ongoingIndexingJobsManager;
        private readonly IAppInsight _appInsight;
        private readonly IInputCoinsRepository _inputCoinsRepository;
        private readonly IUnspentCoinsRepository _unspentCoinsRepository;
        private readonly ISpentCoinsRepository _spentCoinsRepository;
        private readonly IBalanceUpdatesRepository _balanceUpdatesRepository;
        private readonly IFeesRepository _feesRepository;

        public FirstPassBlockDetectedConsumer(ILoggerFactory loggerFactory,
            IBlockHeadersRepository blockHeadersRepository,
            ISecondPassIndexersRepository secondPassIndexersRepository,
            OngoingIndexingJobsManager ongoingIndexingJobsManager,
            IAppInsight appInsight,
            IInputCoinsRepository inputCoinsRepository,
            IUnspentCoinsRepository unspentCoinsRepository,
            ISpentCoinsRepository spentCoinsRepository,
            IBalanceUpdatesRepository balanceUpdatesRepository,
            IFeesRepository feesRepository)
        {
            _loggerFactory = loggerFactory;
            _blockHeadersRepository = blockHeadersRepository;
            _secondPassIndexersRepository = secondPassIndexersRepository;
            _ongoingIndexingJobsManager = ongoingIndexingJobsManager;
            _appInsight = appInsight;
            _inputCoinsRepository = inputCoinsRepository;
            _unspentCoinsRepository = unspentCoinsRepository;
            _spentCoinsRepository = spentCoinsRepository;
            _balanceUpdatesRepository = balanceUpdatesRepository;
            _feesRepository = feesRepository;
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

            var secondPassIndexer = await _secondPassIndexersRepository.Get(evt.BlockchainId);

            var secondPassIndexingResult = await secondPassIndexer.IndexAvailableBlocks(
                _loggerFactory.CreateLogger<SecondPassIndexer>(),
                // TODO: To config
                100,
                _blockHeadersRepository,
                _appInsight,
                _inputCoinsRepository,
                _unspentCoinsRepository,
                _spentCoinsRepository,
                _balanceUpdatesRepository,
                _feesRepository);

            await _secondPassIndexersRepository.Update(secondPassIndexer);

            if (secondPassIndexingResult == SecondPassIndexingResult.IndexingCompleted)
            {
                await _ongoingIndexingJobsManager.EnsureStarted(evt.BlockchainId);
            }
        }
    }
}
