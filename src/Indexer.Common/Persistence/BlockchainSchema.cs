namespace Indexer.Common.Persistence
{
    internal static class BlockchainSchema
    {
        public static string Get(string blockchainId)
        {
            return blockchainId.Replace("-", "_");
        }
    }
}
