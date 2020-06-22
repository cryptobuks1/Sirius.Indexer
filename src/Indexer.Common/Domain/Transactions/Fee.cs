using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Domain.Transactions
{
    public sealed class Fee
    {
        public Fee(string transactionId,
            string blockId,
            Unit unit)
        {
            TransactionId = transactionId;
            BlockId = blockId;
            Unit = unit;
        }

        public string TransactionId { get; }
        public string BlockId { get; }
        public Unit Unit { get; }
    }
}
