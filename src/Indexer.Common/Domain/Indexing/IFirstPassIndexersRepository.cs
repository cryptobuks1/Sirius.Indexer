﻿using System.Threading.Tasks;

namespace Indexer.Common.Domain.Indexing
{
    public interface IFirstPassIndexersRepository
    {
        Task<FirstPassIndexer> Get(FirstPassIndexerId indexerId);
        Task<FirstPassIndexer> GetOrDefault(FirstPassIndexerId id);
        Task Add(FirstPassIndexer indexer);
        Task Update(FirstPassIndexer indexer);
    }
}
