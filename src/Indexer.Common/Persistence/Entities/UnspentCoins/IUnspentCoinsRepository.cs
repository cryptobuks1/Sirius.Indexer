using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Domain.Transactions.Transfers;

namespace Indexer.Common.Persistence.Entities.UnspentCoins
{
    public interface IUnspentCoinsRepository
    {
        Task InsertOrIgnore(string blockchainId, string blockId, IReadOnlyCollection<UnspentCoin> coins);
    }
}
