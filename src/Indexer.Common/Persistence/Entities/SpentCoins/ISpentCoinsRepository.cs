using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Domain.Transactions.Transfers;

namespace Indexer.Common.Persistence.Entities.SpentCoins
{
    public interface ISpentCoinsRepository
    {
        Task InsertOrIgnore(string blockchainId, IReadOnlyCollection<SpentCoin> coins);
        Task<IReadOnlyCollection<SpentCoin>> GetSpentByBlock(string blockchainId, string blockId);
        Task RemoveSpentByBlock(string blockchainId, string blockId);
    }
}
