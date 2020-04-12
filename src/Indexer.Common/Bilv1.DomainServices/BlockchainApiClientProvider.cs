using System.Collections.Concurrent;
using System.Threading.Tasks;
using Indexer.Common.Persistence;
using Lykke.Service.BlockchainApi.Client;
using Microsoft.Extensions.Logging;

namespace Indexer.Common.Bilv1.DomainServices
{
    public class BlockchainApiClientProvider
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IBlockchainsRepository _blockchainsRepository;
        private readonly ConcurrentDictionary<string, IBlockchainApiClient> _clients;

        public BlockchainApiClientProvider(ILoggerFactory loggerFactory, IBlockchainsRepository blockchainsRepository)
        {
            _loggerFactory = loggerFactory;
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

            client = new BlockchainApiClient(_loggerFactory, blockchain.IntegrationUrl);

            _clients.TryAdd(blockchainId, client);

            return client;
        }
    }
}
