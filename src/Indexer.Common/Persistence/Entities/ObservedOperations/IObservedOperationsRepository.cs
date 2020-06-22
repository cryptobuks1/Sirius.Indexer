using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Domain.ObservedOperations;

namespace Indexer.Common.Persistence.Entities.ObservedOperations
{
    public interface IObservedOperationsRepository
    {
        Task AddOrIgnore(ObservedOperation observedOperation);
        Task<IReadOnlyCollection<ObservedOperation>> GetInvolvedInBlock(string blockchainId, string blockId);
    }
}
