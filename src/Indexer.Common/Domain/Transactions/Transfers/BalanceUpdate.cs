using System;

namespace Indexer.Common.Domain.Transactions.Transfers
{
    public sealed class BalanceUpdate
    {
        private BalanceUpdate(string address,
            long assetId,
            long blockNumber,
            string blockId,
            DateTime blockMinedAt,
            decimal amount,
            decimal total)
        {
            Address = address;
            AssetId = assetId;
            BlockNumber = blockNumber;
            BlockId = blockId;
            BlockMinedAt = blockMinedAt;
            Amount = amount;
            Total = total;
        }

        public string Address { get; }
        public long AssetId { get; }
        public long BlockNumber { get; }
        public string BlockId { get; }
        public DateTime BlockMinedAt { get; }
        public decimal Amount { get; }
        public decimal Total { get; }

        public static BalanceUpdate Create(string address,
            long assetId,
            long blockNumber,
            string blockId,
            DateTime blockMinedAt,
            decimal amount)
        {
            return new BalanceUpdate(
                address,
                assetId,
                blockNumber,
                blockId,
                blockMinedAt,
                amount,
                0);
        }
    }
}
