using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Domain.Transactions;

namespace Indexer.Common.Persistence.Entities.TransactionHeaders
{
    public interface ITransactionHeadersRepository
    {
        Task InsertOrIgnore(IReadOnlyCollection<TransactionHeader> transactionHeaders);
        Task RemoveByBlock(string blockchainId, string blockId);
    }
}
