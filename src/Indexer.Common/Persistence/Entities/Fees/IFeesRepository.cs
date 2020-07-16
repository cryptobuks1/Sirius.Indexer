using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Domain.Transactions;

namespace Indexer.Common.Persistence.Entities.Fees
{
    public interface IFeesRepository
    {
        Task InsertOrIgnore(IReadOnlyCollection<Fee> fees);
        Task RemoveByBlock(string blockId);
    }
}
