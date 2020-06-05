using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Persistence.Entities.TransactionHeaders
{
    public class TransactionHeaderEntity
    {
        // ReSharper disable InconsistentNaming
        public string block_id { get; set; }
        public string id { get; set; }
        public int number { get; set; }
        public string error_message { get; set; }
        public TransactionBroadcastingErrorCode? error_code { get; set; }
    }
}
