using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Domain.Transactions.Transfers;

namespace Indexer.Common.Persistence.Entities.InputCoins
{
    public interface IInputCoinsRepository
    {
        Task InsertOrIgnore(IReadOnlyCollection<InputCoin> coins);
        Task<IReadOnlyCollection<InputCoin>> GetByBlock(string blockId);
        Task RemoveByBlock(string blockId);
    }
}
