using System.Collections.Generic;
using System.Threading.Tasks;

namespace Indexer.Common.Domain.Indexing
{
    public interface IOngoingIndexingResult
    {
        IReadOnlyCollection<Task> BackgroundTasks { get; }
        OngoingBlockIndexingResult BlockResult { get; }
    }
}
