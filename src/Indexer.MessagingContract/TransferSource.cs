using Swisschain.Sirius.Sdk.Primitives;

namespace Swisschain.Sirius.Indexer.MessagingContract
{
    public class TransferSource
    {
        public string Address { get; set; }
        public Unit Unit { get; set; }
        public string TransferId { get; set; }

        /// <summary>
        /// Optional
        /// </summary>
        public long? Nonce { get; set; }

        /// <summary>
        /// Optional
        /// </summary>
        public SpentCoin SpentCoin { get; set; }
    }
}
