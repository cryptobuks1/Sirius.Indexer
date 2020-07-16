using System.Threading.Tasks;

namespace Indexer.Common.Domain.Indexing.FirstPass
{
    public interface IFirstPasseIndexingStrategy
    {
        Task IndexNextBlock(FirstPassIndexer indexer);
    }
}