using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Domain.Transactions;

namespace Indexer.Common.Persistence.Entities.Fees
{
    public interface IFeesRepository
    {
        Task InsertOrIgnore(string blockchainId, IReadOnlyCollection<Fee> fees);
    }
}
