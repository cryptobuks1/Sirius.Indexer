using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Domain.Transactions.Transfers
{
    public sealed class MovementDestination
    {
        public MovementDestination(
            BlockchainUnit unit, 
            string transferId, 
            string address = null,
            string tag = null, 
            DestinationTagType? tagType = null)
        {
            Unit = unit;
            TransferId = transferId;
            Address = address;
            Tag = tag;
            TagType = tagType;
        }

        public BlockchainUnit Unit { get; }
        public string TransferId { get; }
        public string Address { get; }
        public string Tag { get; }
        public DestinationTagType? TagType { get; }
    }
}