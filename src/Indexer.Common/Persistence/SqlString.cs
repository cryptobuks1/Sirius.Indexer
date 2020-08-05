namespace Indexer.Common.Persistence
{
    internal static class SqlString
    {
        public static string Escape(string value)
        {
            return value.Replace("'", "''");
        }
    }
}
