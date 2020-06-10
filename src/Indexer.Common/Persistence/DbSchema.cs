namespace Indexer.Common.Persistence
{
    internal static class DbSchema
    {
        public static string GetName(string blockchainId)
        {
            return blockchainId.Replace("-", "_");
        }
    }
}
