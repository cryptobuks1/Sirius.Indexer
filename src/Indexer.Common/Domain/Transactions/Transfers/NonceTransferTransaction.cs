using System.Collections.Generic;

namespace Indexer.Common.Domain.Transactions.Transfers
{
    public sealed class NonceTransferTransaction
    {
        public NonceTransferTransaction(TransactionHeader header, 
            IReadOnlyCollection<MovementSource> sources, 
            IReadOnlyCollection<MovementDestination> destinations)
        {
            Header = header;
            Sources = sources;
            Destinations = destinations;
        }

        public TransactionHeader Header { get; }
        public IReadOnlyCollection<MovementSource> Sources { get; }
        public IReadOnlyCollection<MovementDestination> Destinations { get; }
    }
}