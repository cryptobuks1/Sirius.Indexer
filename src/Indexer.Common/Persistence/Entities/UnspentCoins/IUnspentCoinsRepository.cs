using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Domain.Transactions.Transfers;
using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Persistence.Entities.UnspentCoins
{
    public interface IUnspentCoinsRepository
    {
        Task InsertOrIgnore(string blockchainId, string blockId, IReadOnlyCollection<UnspentCoin> coins);
        Task<IReadOnlyCollection<UnspentCoin>> GetAnyOf(string blockchainId, IReadOnlyCollection<CoinId> ids);
        Task Remove(string blockchainId, IReadOnlyCollection<CoinId> ids);
        Task<IReadOnlyCollection<UnspentCoin>> GetByBlock(string blockchainId, string blockId);
    }
}
