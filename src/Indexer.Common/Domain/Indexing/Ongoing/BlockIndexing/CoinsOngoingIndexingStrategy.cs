using System.Threading.Tasks;
using Indexer.Common.Domain.Blocks;
using Indexer.Common.Domain.Indexing.Common;
using Indexer.Common.Persistence;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Indexer.Common.Domain.Indexing.Ongoing.BlockIndexing
{
    internal sealed class CoinsOngoingIndexingStrategy : IOngoingIndexingStrategy
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IBlocksReader _blocksReader;
        private readonly IBlockchainDbUnitOfWorkFactory _blockchainDbUnitOfWork;
        private readonly UnspentCoinsFactory _unspentCoinsFactory;
        private readonly IPublishEndpoint _publishEndpoint;

        public CoinsOngoingIndexingStrategy(ILoggerFactory loggerFactory, 
            IBlocksReader blocksReader,
            IBlockchainDbUnitOfWorkFactory blockchainDbUnitOfWork,
            UnspentCoinsFactory unspentCoinsFactory,
            IPublishEndpoint publishEndpoint)
        {
            _loggerFactory = loggerFactory;
            _blocksReader = blocksReader;
            _blockchainDbUnitOfWork = blockchainDbUnitOfWork;
            _unspentCoinsFactory = unspentCoinsFactory;
            _publishEndpoint = publishEndpoint;
        }

        public async Task<IOngoingBlockIndexingStrategy> StartBlockIndexing(long blockNumber)
        {
            var block = await _blocksReader.ReadCoinsBlockOrDefault(blockNumber);

            return new CoinsOngoingBlockIndexingStrategy(
                _loggerFactory.CreateLogger<CoinsOngoingBlockIndexingStrategy>(),
                block,
                _blockchainDbUnitOfWork, 
                _unspentCoinsFactory, 
                _publishEndpoint);
        }
    }
}
