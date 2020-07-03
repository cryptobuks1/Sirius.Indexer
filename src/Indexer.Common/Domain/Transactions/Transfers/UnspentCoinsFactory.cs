using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Indexer.Common.Domain.Assets;
using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Domain.Transactions.Transfers
{
    public class UnspentCoinsFactory
    {
        private readonly AssetsManager _assetsManager;

        public UnspentCoinsFactory(AssetsManager assetsManager)
        {
            _assetsManager = assetsManager;
        }

        public async Task<IReadOnlyCollection<UnspentCoin>> Create(IReadOnlyCollection<CoinsTransferTransaction> transfers)
        {
            if (!transfers.Any())
            {
                return Array.Empty<UnspentCoin>();
            }

            var blockchainId = transfers.First().Header.BlockchainId;
            var blockBlockchainAssets = transfers
                .SelectMany(tx => tx.OutputCoins.Select(coin => coin.Unit.Asset))
                .Distinct()
                .ToArray();

            var blockAssets = await _assetsManager.EnsureAdded(blockchainId, blockBlockchainAssets);

            return transfers
                .SelectMany(tx => tx.OutputCoins.Select(coin => new UnspentCoin(
                    new CoinId(tx.Header.Id, coin.Number),
                    new Unit(blockAssets[coin.Unit.Asset.Id].Id, coin.Unit.Amount),
                    coin.Address,
                    coin.ScriptPubKey,
                    coin.Tag,
                    coin.TagType)))
                .ToArray();
        }
    }
}
