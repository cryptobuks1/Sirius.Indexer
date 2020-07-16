using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Domain.Transactions.Transfers.Coins
{
    public sealed class InputCoin
    {
        public InputCoin(CoinId id,
            InputCoinType type,
            CoinId previousOutput)
        {
            Id = id;
            Type = type;
            PreviousOutput = previousOutput;
        }

        public CoinId Id { get; }
        public InputCoinType Type { get; }
        public CoinId PreviousOutput { get; }
    }
}
