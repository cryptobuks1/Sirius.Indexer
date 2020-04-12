using System.Collections.Generic;
using Indexer.Bilv1.Domain.Models.Assets;

namespace Indexer.Bilv1.Domain.Services
{
    public interface IAssetService
    {
        IReadOnlyCollection<Asset> GetAssetsFor(string blockchainId);
        Asset GetAssetForId(string blockchainId, string assetId);
    }
}
