using System.Linq;

namespace Indexer.Common.Domain
{
    public static class StringUtils
    {
        public static string TrimControl(string value)
        {
            var escapedControlSymbols = value.Where(c => c > 0x1f && c != 0x7f).ToArray();

            return new string(escapedControlSymbols);
        }
    }
}
