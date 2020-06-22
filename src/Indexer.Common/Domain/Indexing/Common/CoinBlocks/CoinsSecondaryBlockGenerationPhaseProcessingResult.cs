using System.Collections.Generic;
using Indexer.Common.Domain.Transactions;
using Indexer.Common.Domain.Transactions.Transfers;

namespace Indexer.Common.Domain.Indexing.Common.CoinBlocks
{
    public sealed class CoinsSecondaryBlockGenerationPhaseProcessingResult
    {
        public CoinsSecondaryBlockGenerationPhaseProcessingResult(IReadOnlyCollection<SpentCoin> spentCoins,
            IReadOnlyCollection<Fee> fees,
            IReadOnlyCollection<BalanceUpdate> balanceUpdates)
        {
            SpentCoins = spentCoins;
            Fees = fees;
            BalanceUpdates = balanceUpdates;
        }

        public IReadOnlyCollection<SpentCoin> SpentCoins { get; }
        public IReadOnlyCollection<Fee> Fees { get; }
        public IReadOnlyCollection<BalanceUpdate> BalanceUpdates { get; }
    }
}
