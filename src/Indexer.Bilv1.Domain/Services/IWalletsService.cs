using System.Threading.Tasks;

namespace Indexer.Bilv1.Domain.Services
{
    public interface IWalletsService
    {
        Task ImportWalletAsync(
            string blockchainId,
            string walletAddress);
    }
}
