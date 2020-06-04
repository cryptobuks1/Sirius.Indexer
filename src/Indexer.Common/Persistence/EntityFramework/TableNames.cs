namespace Indexer.Common.Persistence.EntityFramework
{
    public static class TableNames
    {
        public const string BlockHeaders = "block_headers";
        public const string TransactionHeaders = "transaction_headers";
        public const string ObserverOperations = "observed_operations";
        public const string Blockchains = "blockchains";
        public const string FirstPassIndexers = "first_pass_indexers";
        public const string SecondPassIndexers = "second_pass_indexers";
        public const string OngoingIndexers = "ongoing_indexers";
    }
}
