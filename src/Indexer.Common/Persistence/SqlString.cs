using System.Linq;

namespace Indexer.Common.Persistence
{
    internal static class SqlString
    {
        public static string Escape(string value)
        {
            var escapedControlSymbols = value.Where(c => c > 0x1f && c != 0x7f).ToArray();

            return new string(escapedControlSymbols)
                .Replace("'", "''");
        }

        public static string EscapeCopy(string value)
        {
            var escapedControlSymbols = value.Where(c => c > 0x1f && c != 0x7f).ToArray();

            return new string(escapedControlSymbols);
        }
    }
}
