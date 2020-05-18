using System.Threading.Tasks;
using Indexer.Common.Domain.Indexing;

namespace Indexer.Common.Persistence
{
    internal sealed class InMemorySecondPassIndexersRepository : ISecondPassIndexersRepository
    {
        public Task<SecondPassIndexer> Get(string blockchainId)
        {
            throw new System.NotImplementedException();
        }

        public Task<SecondPassIndexer> GetOrDefault(string blockchainId)
        {
            throw new System.NotImplementedException();
        }

        public Task Add(SecondPassIndexer indexer)
        {
            throw new System.NotImplementedException();
        }

        public Task Update(SecondPassIndexer indexer)
        {
            throw new System.NotImplementedException();
        }
    }
}
