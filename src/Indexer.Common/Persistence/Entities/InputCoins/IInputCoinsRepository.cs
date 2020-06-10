using System.Collections.Generic;
using System.Threading.Tasks;
using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Persistence.Entities.InputCoins
{
    public interface IInputCoinsRepository
    {
        Task InsertOrIgnore(string blockchainId, string blockId, IReadOnlyCollection<CoinId> coins);
    }
}
