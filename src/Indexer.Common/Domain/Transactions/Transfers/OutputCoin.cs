using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Domain.Transactions.Transfers
{
    public sealed class OutputCoin
    {
        public OutputCoin(int number, 
            BlockchainUnit unit, 
            string address = null,
            string tag = null, 
            DestinationTagType? tagType = null)
        {
            Number = number;
            Unit = unit;
            Address = address;
            Tag = tag;
            TagType = tagType;
        }

        public int Number { get; }
        public BlockchainUnit Unit { get; }
        public string Address { get; }
        public string Tag { get; }
        public DestinationTagType? TagType { get; }
    }
}
