﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Indexer.Common.Persistence.Entities.Blockchains;
using Microsoft.Extensions.Logging;
using Swisschain.Sirius.Sdk.Integrations.Client;

namespace Indexer.Common.Domain.Blocks
{
    internal sealed class BlockReadersProvider : IBlockReadersProvider, IDisposable
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IBlockchainsRepository _blockchainsRepository;
        private readonly SemaphoreSlim _lock;
        private readonly ConcurrentDictionary<string, IBlocksReader> _blockReaders;
        private readonly ConcurrentBag<ISiriusIntegrationClient> _integrationClients;

        public BlockReadersProvider(ILoggerFactory loggerFactory, IBlockchainsRepository blockchainsRepository)
        {
            _loggerFactory = loggerFactory;
            _blockchainsRepository = blockchainsRepository;

            _lock = new SemaphoreSlim(1, 1);
            _blockReaders = new ConcurrentDictionary<string, IBlocksReader>();
            _integrationClients = new ConcurrentBag<ISiriusIntegrationClient>();
        }

        public async Task<IBlocksReader> Get(string blockchainId)
        {
            var blockchainMetamodel = await _blockchainsRepository.GetAsync(blockchainId);

            await _lock.WaitAsync();

            try
            {
                if (_blockReaders.TryGetValue(blockchainId, out var blocksReader))
                {
                    return blocksReader;
                }
                
                var integrationClient = new SiriusIntegrationClient(blockchainMetamodel.IntegrationUrl, unencrypted: true);

                var blocksReaderImpl = new BlocksReader(
                    _loggerFactory.CreateLogger<BlocksReader>(),
                    integrationClient,
                    blockchainMetamodel);

                blocksReader = new BlocksReaderRetryDecorator(blocksReaderImpl);

                _blockReaders.TryAdd(blockchainId, blocksReader);
                _integrationClients.Add(integrationClient);

                return blocksReader;
            }
            finally
            {
                _lock.Release();
            }
        }

        public void Dispose()
        {
            foreach (var client in _integrationClients.Cast<IDisposable>())
            {
                client.Dispose();
            }

            _lock?.Dispose();
        }
    }
}