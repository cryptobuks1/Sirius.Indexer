using System.Collections.Generic;
using System.Linq;
using Indexer.Bilv1.Domain.Services;
using Indexer.Bilv1.Domain.Models.Assets;

namespace Indexer.Common.Bilv1.DomainServices
{
    public class AssetService : IAssetService
    {
        private readonly Dictionary<string, Asset[]> _assets;

        public AssetService()
        {
            _assets = new Dictionary<string, Asset[]>
            {
                [("bitcoin-regtest")] = new[]
                {
                    new Asset
                    {
                        AssetId = "BTC",
                        Ticker = "BTC",
                        Accuracy = 8
                    }
                },
                [("ethereum-ropsten")] = new[]
                {
                    new Asset
                    {
                        AssetId = "ETH",
                        Ticker = "ETH",
                        Accuracy = 18
                    }
                }
            };
        }

        public IReadOnlyCollection<Asset> GetAssetsFor(string blockchainId)
        {
            _assets.TryGetValue(blockchainId, out var assets);

            return assets ?? new Asset[0];
        }

        public Asset GetAssetForId(string blockchainId, string assetId)
        {
            _assets.TryGetValue(blockchainId, out var assets);

            var result = assets?.SingleOrDefault(x => x.AssetId == assetId);

            return result;
        }
    }
}
