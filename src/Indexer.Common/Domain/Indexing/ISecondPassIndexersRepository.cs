﻿using System.Threading.Tasks;

namespace Indexer.Common.Domain.Indexing
{
    public interface ISecondPassIndexersRepository
    {
        Task<SecondPassIndexer> Get(string blockchainId);
        Task<SecondPassIndexer> GetOrDefault(string blockchainId);
        Task Add(SecondPassIndexer indexer);
        Task Update(SecondPassIndexer indexer);
    }
}
