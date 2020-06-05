using System;
using System.Threading.Tasks;
using Indexer.Common.Domain.Blocks;

namespace Indexer.Common.Domain.Indexing
{
    public sealed class ChainWalker
    {
        private readonly IBlockHeadersRepository _blockHeadersRepository;

        public ChainWalker(IBlockHeadersRepository blockHeadersRepository)
        {
            _blockHeadersRepository = blockHeadersRepository;
        }

        public async Task<ChainWalkerMovement> MoveTo(BlockHeader blockHeader)
        {
            // TODO: Having a cache of the last added block, we can avoid db IO in the most cases for the ongoing indexer

            var previousBlock = await _blockHeadersRepository.GetOrDefault(blockHeader.BlockchainId, blockHeader.Number - 1);

            if (previousBlock == null)
            {
                throw new NotSupportedException("An out-of-order block has been detected");
            }

            if (previousBlock.Id != blockHeader.PreviousId)
            {
                // TODO: Remove rest of the block stuff

                await _blockHeadersRepository.Remove(previousBlock.BlockchainId, previousBlock.Id);

                return ChainWalkerMovement.CreateBackward(previousBlock);
            }

            await _blockHeadersRepository.InsertOrIgnore(blockHeader);

            return ChainWalkerMovement.CreateForward();
        }
    }
}
