using System.Collections.Generic;
using System.Threading.Tasks;

namespace Indexer.Common.Domain.Transactions
{
    public interface ITransactionHeadersRepository
    {
        Task InsertOrIgnore(IReadOnlyCollection<TransactionHeader> transactionHeaders);
        Task RemoveByBlock(string blockchainId, string blockId);
    }
}
