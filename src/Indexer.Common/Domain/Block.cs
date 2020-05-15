namespace Indexer.Common.Domain
{
    public class Block
    {
        public Block(string blockchainId, string id, long number, string previousBlockId)
        {
            GlobalId = $"{blockchainId}-{id}";

            BlockchainId = blockchainId;
            Id = id;
            Number = number;
            PreviousId = previousBlockId;
        }

        public string GlobalId { get; }
        public string BlockchainId { get; }
        public string Id { get; }
        public long Number { get; }
        public string PreviousId { get; }

        public override string ToString()
        {
            return GlobalId;
        }
    }
}
