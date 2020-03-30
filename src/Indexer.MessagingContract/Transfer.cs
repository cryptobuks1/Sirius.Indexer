using Swisschain.Sirius.Sdk.Primitives;

namespace Swisschain.Sirius.Indexer.MessagingContract
{
    public class Transfer
    {
        public int TransferId { get; set; }
        public decimal Amount { get; set; }

        /// <summary>
        /// Optional
        /// </summary>
        public DestinationTag Tag { get; set; }

        /// <summary>
        /// Optional
        /// </summary>
        public DestinationTagType? TagType { get; set; }
        public long Nonce { get; set; }
    }
}
