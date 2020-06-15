using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Domain.Transactions.Transfers;

namespace Indexer.Common.Persistence.Entities.InputCoins
{
    public interface IInputCoinsRepository
    {
        Task InsertOrIgnore(string blockchainId, string blockId, IReadOnlyCollection<InputCoin> coins);
        Task<IReadOnlyCollection<InputCoin>> GetByBlock(string blockchainId, string blockId);
    }
}
