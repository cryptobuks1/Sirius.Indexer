using System;
using System.Threading.Tasks;
using Indexer.Common.Domain.Blocks;
using Indexer.Common.Persistence;
using Microsoft.Extensions.Logging;

namespace Indexer.Common.Domain.Indexing.Ongoing
{
    public sealed class ChainWalker
    {
        private readonly ILogger<ChainWalker> _logger;
        private readonly IBlockchainDbUnitOfWorkFactory _blockchainDbUnitOfWorkFactory;

        public ChainWalker(ILogger<ChainWalker> logger, IBlockchainDbUnitOfWorkFactory blockchainDbUnitOfWorkFactory)
        {
            _logger = logger;
            _blockchainDbUnitOfWorkFactory = blockchainDbUnitOfWorkFactory;
        }

        public async Task<ChainWalkerMovement> MoveTo(BlockHeader blockHeader)
        {
            // TODO: Having a cache of the last added block, we can avoid db IO in the most cases for the ongoing indexer

            await using var unitOfWork = await _blockchainDbUnitOfWorkFactory.Start(blockHeader.BlockchainId);

            var previousBlock = await unitOfWork.BlockHeaders.GetOrDefault(blockHeader.Number - 1);

            if (previousBlock == null)
            {
                _logger.LogError("An out-of-order block has been detected {@context}", new
                {
                    BlockchainId = blockHeader.BlockchainId,
                    BlockNumber = blockHeader.Number
                });

                throw new NotSupportedException("An out-of-order block has been detected");
            }

            if (previousBlock.Id != blockHeader.PreviousId)
            {
                return ChainWalkerMovement.CreateBackward(previousBlock);
            }

            return ChainWalkerMovement.CreateForward();
        }
    }
}
