using System.Threading.Tasks;
using Indexer.Common.Domain.ObservedOperations;

namespace Indexer.Common.Persistence.ObservedOperations
{
    public interface IObservedOperationsRepository
    {
        Task AddOrIgnore(ObservedOperation observedOperation);
    }
}
