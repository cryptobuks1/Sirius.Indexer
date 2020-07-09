using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Domain.Transactions.Transfers.Coins;
using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Persistence.Entities.UnspentCoins
{
    public interface IUnspentCoinsRepository
    {
        Task InsertOrIgnore(IReadOnlyCollection<UnspentCoin> coins);
        Task<IReadOnlyCollection<UnspentCoin>> GetAnyOf(IReadOnlyCollection<CoinId> ids);
        Task Remove(IReadOnlyCollection<CoinId> ids);
        Task<IReadOnlyCollection<UnspentCoin>> GetByBlock(string blockId);
        Task<IReadOnlyCollection<UnspentCoin>> GetByAddress(string address, long? asAtBlockNumber);
        Task RemoveByBlock(string blockId);
    }
}
