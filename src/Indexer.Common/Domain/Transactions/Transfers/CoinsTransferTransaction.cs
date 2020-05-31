using System.Collections.Generic;
using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Domain.Transactions.Transfers
{
    public sealed class CoinsTransferTransaction
    {
        public CoinsTransferTransaction(TransactionHeader header, 
            IReadOnlyCollection<CoinId> inputCoins, 
            IReadOnlyCollection<OutputCoin> outputCoins)
        {
            Header = header;
            InputCoins = inputCoins;
            OutputCoins = outputCoins;
        }

        public TransactionHeader Header { get; }
        public IReadOnlyCollection<CoinId> InputCoins { get; }
        public IReadOnlyCollection<OutputCoin> OutputCoins { get; }
    }
}
