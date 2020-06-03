using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Domain.Transactions;
using Indexer.Common.Durability;
using Polly.Retry;

namespace Indexer.Common.Persistence.RetryDecorators
{
    internal class TransactionHeadersRepositoryRetryDecorator : ITransactionHeadersRepository
    {
        private readonly ITransactionHeadersRepository _impl;
        private readonly AsyncRetryPolicy _retryPolicy;

        public TransactionHeadersRepositoryRetryDecorator(ITransactionHeadersRepository impl)
        {
            _impl = impl;
            _retryPolicy = Policies.DefaultRepositoryRetryPolicy();
        }

        public Task InsertOrIgnore(IEnumerable<TransactionHeader> transactionHeaders)
        {
            return _retryPolicy.ExecuteAsync(() => _impl.InsertOrIgnore(transactionHeaders));
        }

        public Task RemoveByBlock(string blockchainId, string blockId)
        {
            return _retryPolicy.ExecuteAsync(() => _impl.RemoveByBlock(blockchainId, blockId));
        }
    }
}