using System.Collections.Concurrent;
using System.Threading.Tasks;
using Indexer.Common.Persistence;
using Lykke.Service.BlockchainApi.Client;
using Microsoft.Extensions.Logging;

namespace Indexer.Worker.BilV1
{
    public class BilV1ApiClientProvider
    {
        private readonly IBlockchainsRepository _blockchainsRepository;
        private readonly ConcurrentDictionary<string, IBlockchainApiClient> _clients;

        public BilV1ApiClientProvider(IBlockchainsRepository blockchainsRepository)
        {
            _blockchainsRepository = blockchainsRepository;
            _clients = new ConcurrentDictionary<string, IBlockchainApiClient>();
        }

        public async Task<IBlockchainApiClient> GetAsync(string blockchainId)
        {
            if (_clients.TryGetValue(blockchainId, out var client))
            {
                return client;
            }

            var blockchain = await _blockchainsRepository.GetAsync(blockchainId);

            client = new BlockchainApiClient(LoggerFactory.Create(x => x.AddConsole()), blockchain.IntegrationUrl);

            _clients.TryAdd(blockchainId, client);

            return client;
        }
    }
}
