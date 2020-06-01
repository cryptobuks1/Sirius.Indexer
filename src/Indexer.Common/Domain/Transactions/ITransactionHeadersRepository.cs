using System.Collections.Generic;
using System.Threading.Tasks;

namespace Indexer.Common.Domain.Transactions
{
    public interface ITransactionHeadersRepository
    {
        Task InsertOrIgnore(IEnumerable<TransactionHeader> transactionHeaders);
        Task RemoveByBlock(string blockchainId, string blockId);
    }
}
