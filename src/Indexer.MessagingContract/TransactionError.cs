using Swisschain.Sirius.Sdk.Primitives;

namespace Swisschain.Sirius.Indexer.MessagingContract
{
    public class TransactionError
    {
        public string Message { get; set; }
        public TransactionErrorCode Code { get; set; }
    }
}
