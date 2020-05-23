using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Telemetry;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Indexer.Common.Domain.Indexing
{
    public sealed class SecondPassHistoryIndexer
    {
        private SecondPassHistoryIndexer(string blockchainId,
            long nextBlock,
            long stopBlock,
            long version)
        {
            BlockchainId = blockchainId;
            NextBlock = nextBlock;
            StopBlock = stopBlock;
            Version = version;
        }

        public string BlockchainId { get; }
        public long NextBlock { get; private set; }
        public long StopBlock { get; }
        public long Version { get; }
        public bool IsCompleted => NextBlock == StopBlock;

        public static SecondPassHistoryIndexer Create(string blockchainId, long startBlock, long stopBlock)
        {
            return new SecondPassHistoryIndexer(
                blockchainId,
                startBlock,
                stopBlock,
                version: 0);
        }

        public async Task<SecondPassHistoryIndexingResult> IndexAvailableBlocks(
            ILogger<SecondPassHistoryIndexer> logger,
            int maxBlocksCount,
            IBlocksRepository blocksRepository,
            IPublishEndpoint publisher,
            IAppInsight appInsight)
        {
            if (IsCompleted)
            {
                return SecondPassHistoryIndexingResult.IndexingCompleted;
            }

            var blocks = await blocksRepository.GetBatch(BlockchainId, NextBlock, maxBlocksCount);

            try
            {
                foreach (var block in blocks)
                {
                    if (NextBlock != block.Number)
                    {
                        return SecondPassHistoryIndexingResult.IndexingInProgress;
                    }

                    await StepForward(block, publisher, appInsight);

                    if (IsCompleted)
                    {
                        return SecondPassHistoryIndexingResult.IndexingCompleted;
                    }
                }
            }
            finally
            {
                logger.LogInformation("Second-pass indexer has processed the blocks batch {@context}", this);
            }

            return SecondPassHistoryIndexingResult.IndexingInProgress;
        }
        
        private async Task StepForward(Block block, IPublishEndpoint publisher, IAppInsight appInsight)
        {
            var appInsightOperation = appInsight.StartRequest("Second-pass block indexing",
                new Dictionary<string, string>
                {
                    ["blockchainId"] = BlockchainId,
                    ["nextBlock"] = NextBlock.ToString()
                });

            try
            {

                // TODO: Index block data

                NextBlock = block.Number + 1;
            }
            catch (Exception ex)
            {
                appInsightOperation.Fail(ex);

                throw;
            }
            finally
            {
                appInsightOperation.Stop();
            }
        }
    }
}
