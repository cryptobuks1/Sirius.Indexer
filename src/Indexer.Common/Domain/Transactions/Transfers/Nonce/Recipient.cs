using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Domain.Transactions.Transfers.Nonce
{
    public sealed class Recipient
    {
        public Recipient(string address, string tag, DestinationTagType? tagType)
        {
            Address = address;
            Tag = tag;
            TagType = tagType;
        }

        public string Address { get; }
        public string Tag { get; }
        public DestinationTagType? TagType { get; }
    }
}
