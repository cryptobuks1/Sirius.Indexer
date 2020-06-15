namespace Indexer.Common.Domain.Transactions
{
    public sealed class Fee
    {
        public Fee(string transactionId,
            long assetId,
            string blockId,
            decimal amount)
        {
            TransactionId = transactionId;
            AssetId = assetId;
            BlockId = blockId;
            Amount = amount;
        }

        public string TransactionId { get; }
        public long AssetId { get; }
        public string BlockId { get; }
        public decimal Amount { get; }
    }
}
