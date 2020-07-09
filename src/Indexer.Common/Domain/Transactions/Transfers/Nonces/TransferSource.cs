using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Domain.Transactions.Transfers.Nonces
{
    public sealed class TransferSource
    {
        public TransferSource(Sender sender, BlockchainUnit unit)
        {
            Sender = sender;
            Unit = unit;
        }

        public Sender Sender { get; }
        public BlockchainUnit Unit { get; }
    }
}
