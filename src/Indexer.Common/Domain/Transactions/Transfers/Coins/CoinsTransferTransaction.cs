using System.Collections.Generic;

namespace Indexer.Common.Domain.Transactions.Transfers.Coins
{
    public sealed class CoinsTransferTransaction
    {
        public CoinsTransferTransaction(TransactionHeader header, 
            IReadOnlyCollection<InputCoin> inputCoins, 
            IReadOnlyCollection<OutputCoin> outputCoins)
        {
            Header = header;
            InputCoins = inputCoins;
            OutputCoins = outputCoins;
        }

        public TransactionHeader Header { get; }
        public IReadOnlyCollection<InputCoin> InputCoins { get; }
        public IReadOnlyCollection<OutputCoin> OutputCoins { get; }
    }
}
