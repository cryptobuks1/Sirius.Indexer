using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Persistence.Entities
{
    public class TransactionHeaderEntity
    {
        public string GlobalId { get; set; }
        public string BlockchainId { get; set; }
        public string BlockId { get; set; }
        public string Id { get; set; }
        public int Number { get; set; }
        public string ErrorMessage { get; set; }
        public TransactionBroadcastingErrorCode? ErrorCode { get; set; }
    }
}
