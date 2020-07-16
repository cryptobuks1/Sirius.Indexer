using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Domain.Transactions;

namespace Indexer.Common.Persistence.Entities.Fees
{
    internal sealed class FeesRepositoryRetryDecorator : IFeesRepository
    {
        private readonly IFeesRepository _impl;

        public FeesRepositoryRetryDecorator(IFeesRepository impl)
        {
            _impl = impl;
        }

        public Task InsertOrIgnore(IReadOnlyCollection<Fee> fees)
        {
            return _impl.InsertOrIgnore(fees);
        }

        public Task RemoveByBlock(string blockId)
        {
            return _impl.RemoveByBlock(blockId);
        }
    }
}
