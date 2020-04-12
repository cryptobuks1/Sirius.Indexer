using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Indexer.Common.Persistence;
using Lykke.Service.BlockchainApi.Client;
using Microsoft.Extensions.Logging;

namespace Indexer.Common.Bilv1.DomainServices
{
    public class BlockchainApiClientProvider
    {
        private readonly Lazy<Task<IDictionary<string, IBlockchainApiClient>>> _clients;

        public BlockchainApiClientProvider(
            ILoggerFactory loggerFactory,
            IBlockchainsRepository blockchainsRepository)
        {
            _clients = new Lazy<Task<IDictionary<string, IBlockchainApiClient>>>(
                () => GetAllBlockchains(loggerFactory, blockchainsRepository));
        }

        public async Task<IBlockchainApiClient> Get(string blockchainType)
        {
            var clients = await _clients.Value;
            if (!clients.TryGetValue(blockchainType, out var client))
            {
                throw new InvalidOperationException($"Blockchain API client [{blockchainType}] is not found");
            }

            return client;
        }

        private static async Task<IDictionary<string, IBlockchainApiClient>> GetAllBlockchains(ILoggerFactory loggerFactory,
            IBlockchainsRepository blockchainsRepository)
        {
            return (await blockchainsRepository.GetAllAsync())
                .ToDictionary(
                    x => x.BlockchainId,
                    x => (IBlockchainApiClient)new BlockchainApiClient(loggerFactory, x.IntegrationUrl));
        }
    }
}
