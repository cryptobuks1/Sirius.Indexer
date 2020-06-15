namespace Indexer.Common.Persistence
{
    public static class TableNames
    {
        public const string Assets = "assets";
        public const string BlockHeaders = "block_headers";
        public const string TransactionHeaders = "transaction_headers";
        public const string InputCoins = "input_coins";
        public const string UnspentCoins = "unspent_coins";
        public const string SpentCoins = "spent_coins";
        public const string BalanceUpdates = "balance_updates";
        public const string Fees = "fees";
        public const string ObserverOperations = "observed_operations";
        public const string Blockchains = "blockchains";
        public const string FirstPassIndexers = "first_pass_indexers";
        public const string SecondPassIndexers = "second_pass_indexers";
        public const string OngoingIndexers = "ongoing_indexers";
    }
}
