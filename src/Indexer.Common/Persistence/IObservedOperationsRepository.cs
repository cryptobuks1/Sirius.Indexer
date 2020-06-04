using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Domain.ObservedOperations;

namespace Indexer.Common.Persistence.ObservedOperations
{
    public interface IObservedOperationsRepository
    {
        Task AddOrIgnore(ObservedOperation observedOperation);
        Task<IReadOnlyCollection<ObservedOperation>> GetExecutingAsync(long? cursor, int count);
        Task UpdateBatchAsync(IReadOnlyCollection<ObservedOperation> updatedOperations);
    }
}
