using System.Linq;
using Swisschain.Sirius.Sdk.Crypto.AddressFormatting;
using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Domain.Blocks
{
    internal static class AddressFormatterExtensions
    {
        public static string NormalizeOrPassThrough(this IAddressFormatter formatter, string address, NetworkType networkType)
        {
            if (address == null)
            {
                return null;
            }

            return formatter.GetFormats(address, networkType).FirstOrDefault()?.Address ?? address;
        }
    }
}
