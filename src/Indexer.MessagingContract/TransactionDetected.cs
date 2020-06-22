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
        public long? OperationId { get; set; }
        public IReadOnlyCollection<TransferSource> Sources { get; set; }
        public IReadOnlyCollection<TransferDestination> Destinations { get; set; }
        /* 
        SpentCoin[] SpentCoins // can be added when needed
        OutputCoin[] OutputCoins // can be added when needed
        NonceUpdate[] NonceUpdates // can be added when needed
        */
        public IReadOnlyCollection<Unit> Fees { get; set; }
        public TransactionError Error { get; set; }
    }
}
