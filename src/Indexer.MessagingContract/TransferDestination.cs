using Swisschain.Sirius.Sdk.Primitives;

namespace Swisschain.Sirius.Indexer.MessagingContract
{
    public class TransferDestination
    {
        public string Address { get; set; }
        public Unit Unit { get; set; }

        /// <summary>
        /// Optional
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        /// Optional
        /// </summary>
        public DestinationTagType? TagType { get; set; }
    }
}
