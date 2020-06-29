namespace Swisschain.Sirius.Indexer.ApiContract.Common
{
    public partial class CoinId
    {
        public static implicit operator Swisschain.Sirius.Sdk.Primitives.CoinId(CoinId value)
        {
            return value != null
                ? new Swisschain.Sirius.Sdk.Primitives.CoinId(value.TransactionId, value.Number)
                : null;
        }

        public static implicit operator CoinId(Swisschain.Sirius.Sdk.Primitives.CoinId value)
        {
            return value != null
                ? new CoinId
                {
                    TransactionId = value.TransactionId,
                    Number = value.Number
                }
                : null;
        }
    }
}
