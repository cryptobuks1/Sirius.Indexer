﻿using System.Threading.Tasks;

namespace Indexer.Common.Domain.Indexing
{
    public interface IOngoingIndexersRepository
    {
        Task<OngoingIndexer> Get(string blockchainId);
        Task Update(OngoingIndexer indexer);
        Task<OngoingIndexer> GetOrDefault(string blockchainId);
        Task Add(OngoingIndexer indexer);
    }
}