namespace Swisschain.Sirius.Indexer.ApiContract.Common
{
    public partial class Unit
    {
        public static implicit operator Swisschain.Sirius.Sdk.Primitives.Unit(Unit value)
        {
            return value != null
                ? new Swisschain.Sirius.Sdk.Primitives.Unit(value.AssetId, value.Amount)
                : null;
        }

        public static implicit operator Unit(Swisschain.Sirius.Sdk.Primitives.Unit value)
        {
            return value != null
                ? new Unit
                {
                    AssetId = value.AssetId,
                    Amount = value.Amount
                }
                : null;
        }
    }
}
