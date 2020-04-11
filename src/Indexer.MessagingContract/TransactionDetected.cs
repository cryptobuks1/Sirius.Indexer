using System.Collections.Generic;
using Swisschain.Sirius.Sdk.Primitives;

namespace Swisschain.Sirius.Indexer.MessagingContract
{
    public class TransactionDetected
    {
        public string BlockchainId { get; set; }
        public string BlockId { get; set; }
        public long BlockNumber { get; set; }
        public string TransactionId { get; set; }
        public int TransactionNumber { get; set; }
        
        /// <summary>
        /// Optional
        /// </summary>
        public long? OperationId { get; set; }
        public IReadOnlyCollection<TransferSource> Sources { get; set; }
        public IReadOnlyCollection<TransferDestination> Destinations { get; set; }
        
        /// <summary>
        /// Optional
        /// </summary>
        public TransactionError Error { get; set; }
        public IReadOnlyCollection<Unit> Fees { get; set; }

        
    }
}
