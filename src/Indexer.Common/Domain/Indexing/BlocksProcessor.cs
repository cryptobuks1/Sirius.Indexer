using System;
using System.Threading.Tasks;

namespace Indexer.Common.Domain.Indexing
{
    public sealed class BlocksProcessor
    {
        private readonly IBlocksRepository _blocksRepository;

        public BlocksProcessor(IBlocksRepository blocksRepository)
        {
            _blocksRepository = blocksRepository;
        }

        public async Task<BlockProcessingResult> ProcessBlock(Block block)
        {
            // TODO: Having a cache of the last added block, we can avoid db IO in the most cases for the ongoing indexer

            var previousBlock = await _blocksRepository.GetOrDefault(block.BlockchainId, block.Number - 1);

            if (previousBlock == null)
            {
                throw new NotSupportedException("An out-of-order block has been detected");
            }

            if (previousBlock.Id != block.PreviousId)
            {
                await _blocksRepository.Remove(previousBlock.GlobalId);

                return BlockProcessingResult.CreateBackward(previousBlock);
            }

            await _blocksRepository.InsertOrIgnore(block);

            return BlockProcessingResult.CreateForward();
        }
    }
}
