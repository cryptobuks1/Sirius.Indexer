using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Domain.Transactions.Transfers.Nonce
{
    public sealed class TransferDestination
    {
        public TransferDestination(Recipient recipient, BlockchainUnit unit)
        {
            Recipient = recipient;
            Unit = unit;
        }

        public Recipient Recipient { get; }
        public BlockchainUnit Unit { get; }
    }
}
