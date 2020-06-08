using System;
using System.Threading.Tasks;
using Indexer.Common.Domain.Blocks;
using Indexer.Common.Persistence.Entities.BlockHeaders;
using Microsoft.Extensions.Logging;

namespace Indexer.Common.Domain.Indexing.Ongoing
{
    public sealed class ChainWalker
    {
        private readonly ILogger<ChainWalker> _logger;
        private readonly IBlockHeadersRepository _blockHeadersRepository;

        public ChainWalker(ILogger<ChainWalker> logger, IBlockHeadersRepository blockHeadersRepository)
        {
            _logger = logger;
            _blockHeadersRepository = blockHeadersRepository;
        }

        public async Task<ChainWalkerMovement> MoveTo(BlockHeader blockHeader)
        {
            // TODO: Having a cache of the last added block, we can avoid db IO in the most cases for the ongoing indexer

            var previousBlock = await _blockHeadersRepository.GetOrDefault(blockHeader.BlockchainId, blockHeader.Number - 1);

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
