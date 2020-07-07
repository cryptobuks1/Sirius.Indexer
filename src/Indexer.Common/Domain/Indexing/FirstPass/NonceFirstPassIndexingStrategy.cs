using System.Threading.Tasks;

namespace Indexer.Common.Domain.Indexing.FirstPass
{
    internal class NonceFirstPassIndexingStrategy : IFirstPasseIndexingStrategy
    {
        public Task IndexNextBlock(FirstPassIndexer indexer)
        {
            throw new System.NotImplementedException();
        }
    }
}
