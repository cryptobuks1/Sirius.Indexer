using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Domain.Blocks;
using Indexer.Common.Domain.Indexing.Common.CoinBlocks;
using Indexer.Common.Persistence.Entities.BlockHeaders;
using Indexer.Common.Telemetry;
using Microsoft.Extensions.Logging;

namespace Indexer.Common.Domain.Indexing.SecondPass
{
    public sealed class SecondPassIndexer
    {
        private SecondPassIndexer(string blockchainId,
            long nextBlock,
            long stopBlock,
            DateTime startedAt,
            DateTime updatedAt,
            int version)
        {
            BlockchainId = blockchainId;
            NextBlock = nextBlock;
            StopBlock = stopBlock;
            StartedAt = startedAt;
            UpdatedAt = updatedAt;
            Version = version;
        }

        public string BlockchainId { get; }
        public long NextBlock { get; private set; }
        public long StopBlock { get; }
        public DateTime StartedAt { get; }
        public DateTime UpdatedAt { get; private set; }
        public int Version { get; }
        public bool IsCompleted => NextBlock >= StopBlock;

        public static SecondPassIndexer Start(string blockchainId, long startBlock, long stopBlock)
        {
            var now = DateTime.UtcNow;

            return new SecondPassIndexer(
                blockchainId,
                startBlock,
                stopBlock,
                now,
                now,
                version: 0);
        }

        public static SecondPassIndexer Restore(string blockchainId,
            long nextBlock,
            long stopBlock,
            DateTime startedAt,
            DateTime updatedAt,
            int version)
        {
            return new SecondPassIndexer(
                blockchainId,
                nextBlock,
                stopBlock,
                startedAt,
                updatedAt,
                version);
        }

        public async Task<SecondPassIndexingResult> IndexAvailableBlocks(
            ILogger<SecondPassIndexer> logger,
            int maxBlocksCount,
            IBlockHeadersRepository blockHeadersRepository,
            IAppInsight appInsight, 
            CoinsSecondaryBlockProcessor coinsSecondaryBlockProcessor)
        {
            if (IsCompleted)
            {
                return SecondPassIndexingResult.IndexingCompleted;
            }

            var blocks = await blockHeadersRepository.GetBatch(BlockchainId, NextBlock, maxBlocksCount);
            var processedBlocksCount = 0;

            try
            {
                foreach (var block in blocks)
                {
                    if (NextBlock != block.Number)
                    {
                        return SecondPassIndexingResult.IndexingInProgress;
                    }

                    await IndexBlock(block, appInsight, coinsSecondaryBlockProcessor);

                    ++processedBlocksCount;

                    if (IsCompleted)
                    {
                        return SecondPassIndexingResult.IndexingCompleted;
                    }
                }
            }
            finally
            {
                logger.LogInformation("Second-pass indexer has processed the blocks batch {@context}. Processed blocks count: {@processedBlocksCount}", this, processedBlocksCount);
            }

            return SecondPassIndexingResult.IndexingInProgress;
        }
        
        private async Task IndexBlock(BlockHeader blockHeader,
            IAppInsight appInsight, 
            CoinsSecondaryBlockProcessor coinsSecondaryBlockProcessor)
        {
            var telemetry = appInsight.StartRequest("Second-pass block indexing",
                new Dictionary<string, string>
                {
                    ["blockchainId"] = BlockchainId,
                    ["nextBlock"] = NextBlock.ToString()
                });

            try
            {
                await coinsSecondaryBlockProcessor.Process(blockHeader);

                NextBlock = blockHeader.Number + 1;
                UpdatedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                telemetry.Fail(ex);

                throw;
            }
            finally
            {
                telemetry.Stop();
            }
        }
    }
}
