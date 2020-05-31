using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Domain.Transactions.Transfers
{
    public sealed class MovementSource
    {
        public MovementSource(BlockchainUnit unit,
            string transferId,
            string address = null,
            long nonce = 0)
        {
            Unit = unit;
            TransferId = transferId;
            Address = address;
            Nonce = nonce;
        }

        public BlockchainUnit Unit { get; }
        public string TransferId { get; }
        public string Address { get; }
        public long Nonce { get; }
    }
}