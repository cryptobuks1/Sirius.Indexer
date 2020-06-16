﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Domain.Transactions.Transfers;
using Indexer.Common.Durability;
using Polly.Retry;
using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Persistence.Entities.UnspentCoins
{
    internal sealed class UnspentCoinsRepositoryRetryDecorator : IUnspentCoinsRepository
    {
        private readonly IUnspentCoinsRepository _impl;
        private readonly AsyncRetryPolicy _retryPolicy;

        public UnspentCoinsRepositoryRetryDecorator(IUnspentCoinsRepository impl)
        {
            _impl = impl;
            _retryPolicy = Policies.DefaultRepositoryRetryPolicy();
        }

        public Task InsertOrIgnore(string blockchainId, string blockId, IReadOnlyCollection<UnspentCoin> coins)
        {
            return _retryPolicy.ExecuteAsync(() => _impl.InsertOrIgnore(blockchainId, blockId, coins));
        }

        public Task<IReadOnlyCollection<UnspentCoin>> GetAnyOf(string blockchainId, IReadOnlyCollection<CoinId> ids)
        {
            return _impl.GetAnyOf(blockchainId, ids);
        }

        public Task Remove(string blockchainId, IReadOnlyCollection<CoinId> ids)
        {
            return _impl.Remove(blockchainId, ids);
        }

        public Task<IReadOnlyCollection<UnspentCoin>> GetByBlock(string blockchainId, string blockId)
        {
            return _impl.GetByBlock(blockchainId, blockId);
        }
    }
}
