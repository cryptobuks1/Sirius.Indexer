using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Bilv1.Domain.Models.EnrolledBalances;
using Indexer.Bilv1.Domain.Models.Operations;

namespace Indexer.Bilv1.Domain.Repositories
{
    public interface IOperationRepository
    {
        Task<Operation> AddAsync(DepositWalletKey key, decimal balanceChange, long block);

        Task<IEnumerable<Operation>> GetAsync(DepositWalletKey key, int skip, int take);

        Task<IEnumerable<Operation>> GetAsync(string blockchainId, string walletAddress, int skip, int take);

        Task<IEnumerable<Operation>> GetAllForBlockchainAsync(string blockchainId, int skip, int take);
    }
}
