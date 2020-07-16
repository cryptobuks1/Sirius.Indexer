using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Domain.Transactions.Transfers.Coins;

namespace Indexer.Common.Persistence.Entities.SpentCoins
{
    public interface ISpentCoinsRepository
    {
        Task InsertOrIgnore(IReadOnlyCollection<SpentCoin> coins);
        Task<IReadOnlyCollection<SpentCoin>> GetSpentByBlock(string blockId);
        Task RemoveSpentByBlock(string blockId);
    }
}
