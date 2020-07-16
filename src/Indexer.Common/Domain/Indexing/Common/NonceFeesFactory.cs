using System;
using System.Collections.Generic;
using System.Linq;
using Indexer.Common.Domain.Assets;
using Indexer.Common.Domain.Transactions;
using Indexer.Common.Domain.Transactions.Transfers.Nonce;
using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Domain.Indexing.Common
{
    public static class NonceFeesFactory
    {
        public static IReadOnlyCollection<Fee> Create(
            IReadOnlyCollection<NonceTransferTransaction> transfers,
            IReadOnlyDictionary<BlockchainAssetId, Asset> assets)
        {
            if (!transfers.Any())
            {
                return Array.Empty<Fee>();
            }
            
            return transfers
                .SelectMany(tx => tx.Fees
                    .Where(feeSource => feeSource.BlockchainUnit.Amount > 0)
                    .Select(feeSource => new Fee(
                        tx.Header.Id,
                        tx.Header.BlockId,
                        new Unit(assets[feeSource.BlockchainUnit.Asset.Id].Id, feeSource.BlockchainUnit.Amount))))
                .ToArray();
        }
    }
}
