using System.Collections.Generic;
using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Domain.Transactions.Transfers.Nonces
{
    public sealed class NonceTransferTransaction
    {
        public NonceTransferTransaction(TransactionHeader header, 
            IReadOnlyCollection<TransferOperation> operations,
            IReadOnlyCollection<NonceUpdate> nonceUpdates,
            IReadOnlyCollection<FeeSource> fees)
        {
            Header = header;
            Operations = operations;
            NonceUpdates = nonceUpdates;
            Fees = fees;
        }

        public TransactionHeader Header { get; }
        public IReadOnlyCollection<TransferOperation> Operations { get; }
        public IReadOnlyCollection<NonceUpdate> NonceUpdates { get; }
        public IReadOnlyCollection<FeeSource> Fees { get; }
    }
}
