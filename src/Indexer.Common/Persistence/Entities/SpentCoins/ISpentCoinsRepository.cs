using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Domain.Transactions.Transfers;

namespace Indexer.Common.Persistence.Entities.SpentCoins
{
    public interface ISpentCoinsRepository
    {
        Task InsertOrIgnore(string blockchainId, string blockId, IReadOnlyCollection<SpentCoin> coins);
    }
}
