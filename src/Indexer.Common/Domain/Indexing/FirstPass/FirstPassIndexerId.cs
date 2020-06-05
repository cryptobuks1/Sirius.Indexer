using System;

namespace Indexer.Common.Domain.Indexing.FirstPass
{
    public sealed class FirstPassIndexerId : IEquatable<FirstPassIndexerId>
    {
        public FirstPassIndexerId(string blockchainId, long startBlock)
        {
            BlockchainId = blockchainId;
            StartBlock = startBlock;
        }

        public string BlockchainId { get; }
        public long StartBlock { get; }

        public override string ToString()
        {
            return $"{BlockchainId}-{StartBlock}";
        }

        public bool Equals(FirstPassIndexerId other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return BlockchainId == other.BlockchainId && StartBlock == other.StartBlock;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is FirstPassIndexerId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(BlockchainId, StartBlock);
        }
    }
}
