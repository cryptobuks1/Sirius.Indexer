using System;
using System.Threading.Tasks;

namespace Indexer.Common.Domain.Indexing
{
    public sealed class BlockProcessor
    {
        private readonly IBlocksRepository _blocksRepository;

        public BlockProcessor(IBlocksRepository blocksRepository)
        {
            _blocksRepository = blocksRepository;
        }

        public async Task<BlockProcessingResult> ProcessBlock(long startBlockNumber, Block block)
        {
            if (block.Number == startBlockNumber)
            {
                await _blocksRepository.InsertOrReplace(block);

                return BlockProcessingResult.CreateForward();
            }

            var previousBlock = await _blocksRepository.GetOrDefault(block.BlockchainId, block.Number - 1);

            if (previousBlock == null)
            {
                throw new NotSupportedException("An out-of-order block has been processed");
            }

            if (previousBlock.Id != block.PreviousId)
            {
                await _blocksRepository.Remove(previousBlock.GlobalId);

                return BlockProcessingResult.CreateBackward(previousBlock);
            }

            await _blocksRepository.InsertOrReplace(block);

            return BlockProcessingResult.CreateForward();
        }
    }
}
