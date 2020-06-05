using System;

namespace Indexer.Common.Domain.Blocks
{
    public sealed class BlockHeader
    {
        public BlockHeader(string blockchainId, string id, long number, string previousBlockId, DateTime minedAt)
        {
            BlockchainId = blockchainId;
            Id = id;
            Number = number;
            PreviousId = previousBlockId;
            MinedAt = minedAt;
        }

        public string BlockchainId { get; }
        public string Id { get; }
        public long Number { get; }
        public string PreviousId { get; }
        public DateTime MinedAt { get; }

        public override string ToString()
        {
            return $"{BlockchainId}:{Id}";
        }
    }
}
