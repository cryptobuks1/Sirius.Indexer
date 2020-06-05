namespace Indexer.Common.Persistence.Entities.Blockchains
{
    internal static class BlockchainSchema
    {
        public static string Get(string blockchainId)
        {
            return blockchainId.Replace("-", "_");
        }
    }
}
