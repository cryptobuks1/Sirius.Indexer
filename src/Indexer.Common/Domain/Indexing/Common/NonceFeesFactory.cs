using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Indexer.Common.Domain.Assets;
using Indexer.Common.Domain.Transactions;
using Indexer.Common.Domain.Transactions.Transfers.Nonce;
using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Domain.Indexing.Common
{
    public class NonceFeesFactory
    {
        private readonly AssetsManager _assetsManager;

        public NonceFeesFactory(AssetsManager assetsManager)
        {
            _assetsManager = assetsManager;
        }

        public async Task<IReadOnlyCollection<Fee>> Create(IReadOnlyCollection<NonceTransferTransaction> transfers)
        {
            if (!transfers.Any())
            {
                return Array.Empty<Fee>();
            }

            var blockchainId = transfers.First().Header.BlockchainId;
            var blockBlockchainAssets = transfers
                .SelectMany(tx => tx.Fees.Select(feeSource => feeSource.BlockchainUnit.Asset))
                .Distinct()
                .ToArray();
            var blockAssets = await _assetsManager.EnsureAdded(blockchainId, blockBlockchainAssets);

            return transfers
                .SelectMany(tx => tx.Fees
                    .Where(feeSource => feeSource.BlockchainUnit.Amount > 0)
                    .Select(feeSource => new Fee(
                        tx.Header.Id,
                        tx.Header.BlockId,
                        new Unit(blockAssets[feeSource.BlockchainUnit.Asset.Id].Id, feeSource.BlockchainUnit.Amount))))
                .ToArray();
        }
    }
}
