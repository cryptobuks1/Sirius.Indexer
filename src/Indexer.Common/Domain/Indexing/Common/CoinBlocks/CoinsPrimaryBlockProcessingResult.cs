using System.Collections.Generic;
using Indexer.Common.Domain.Transactions.Transfers;

namespace Indexer.Common.Domain.Indexing.Common.CoinBlocks
{
    public sealed class CoinsPrimaryBlockProcessingResult
    {
        public CoinsPrimaryBlockProcessingResult(IReadOnlyCollection<UnspentCoin> unspentCoins)
        {
            UnspentCoins = unspentCoins;
        }

        public IReadOnlyCollection<UnspentCoin> UnspentCoins { get; }
    }
}
