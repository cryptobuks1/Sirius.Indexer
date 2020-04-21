using System.Globalization;

namespace Swisschain.Sirius.Indexer.ApiContract.Common
{
    public partial class BigDecimal
    {
        public static implicit operator decimal(BigDecimal value)
        {
            return decimal.Parse(value.Value, CultureInfo.InvariantCulture);
        }

        public static implicit operator BigDecimal(decimal value)
        {
            return new BigDecimal {Value = value.ToString(CultureInfo.InvariantCulture)};
        }
    }
}
