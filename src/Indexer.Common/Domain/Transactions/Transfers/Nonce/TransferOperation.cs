using System.Collections.Generic;

namespace Indexer.Common.Domain.Transactions.Transfers.Nonce
{
    public sealed class TransferOperation
    {
        public TransferOperation(string id, 
            string type,
            IReadOnlyCollection<TransferSource> sources, 
            IReadOnlyCollection<TransferDestination> destinations)
        {
            Id = id;
            Type = type;
            Sources = sources;
            Destinations = destinations;
        }

        public string Id { get; }
        public string Type { get; }
        public IReadOnlyCollection<TransferSource> Sources { get; }
        public IReadOnlyCollection<TransferDestination> Destinations { get; }
    }
}
