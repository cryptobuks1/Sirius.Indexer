using System.Collections.Generic;
using System.Threading.Tasks;

namespace Indexer.Common.Domain.Indexing.Ongoing
{
    internal sealed class OngoingIndexingResult : IOngoingIndexingResult
    {
        private readonly List<Task> _backgroundTasks = new List<Task>();

        private OngoingIndexingResult(OngoingBlockIndexingResult blockResult)
        {
            BlockResult = blockResult;
        }

        public IReadOnlyCollection<Task> BackgroundTasks => _backgroundTasks;
        public OngoingBlockIndexingResult BlockResult { get; }

        public static OngoingIndexingResult BlockNotFound()
        {
            return new OngoingIndexingResult(OngoingBlockIndexingResult.BlockNotFound);
        }

        public static OngoingIndexingResult BlockIndexed()
        {
            return new OngoingIndexingResult(OngoingBlockIndexingResult.BlockIndexed);
        }

        public void AddBackgroundTask(Task task)
        {
            _backgroundTasks.Add(task);
        }
    }
}
