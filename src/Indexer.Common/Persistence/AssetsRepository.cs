using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Indexer.Common.Domain;
using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Persistence
{
    internal class AssetsRepository : IAssetsRepository
    {
        private readonly ImmutableDictionary<AssetId, Asset> _store;

        public AssetsRepository()
        {
            _store = new Dictionary<AssetId, Asset>
            {
                ["100000"] = new Asset(100000,
                    "bitcoin-regtest",
                    "BTC",
                    null,
                    8),
                ["100001"] = new Asset(100001,
                    "ethereum-ropsten",
                    "ETH",
                    null,
                    18),
                ["100002"] = new Asset(100002,
                    "ethereum-ropsten",
                    "TST",
                    "0x722dd3f80bac40c951b51bdd28dd19d435762180",
                    18)
            }.ToImmutableDictionary();
        }

        public Task<IReadOnlyCollection<Asset>> GetAllAsync()
        {
            return Task.FromResult<IReadOnlyCollection<Asset>>(_store.Values.ToArray());
        }
    }
}
