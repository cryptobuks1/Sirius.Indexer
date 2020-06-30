using System;
using System.Linq;
using System.Threading.Tasks;
using Indexer.Common.Domain.Blocks;
using Indexer.Common.Domain.Indexing.Common;
using Indexer.Common.Persistence;
using Microsoft.Extensions.Logging;
using Swisschain.Sirius.Sdk.Primitives;

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
            IBlockchainDbUnitOfWorkFactory blockchainDbUnitOfWorkFactory)
        {
            if (IsCompleted)
            {
                return SecondPassIndexingResult.IndexingCompleted;
            }

            await using var unitOfWork = await blockchainDbUnitOfWorkFactory.Start(BlockchainId);

            var blockHeaders = await unitOfWork.BlockHeaders.GetBatch(NextBlock, maxBlocksCount);
            var processedBlocksCount = 0;

            try
            {
                foreach (var blockHeader in blockHeaders)
                {
                    if (NextBlock != blockHeader.Number)
                    {
                        return SecondPassIndexingResult.IndexingInProgress;
                    }

                    await IndexBlock(blockHeader, unitOfWork);

                    ++processedBlocksCount;

                    if (IsCompleted)
                    {
                        return SecondPassIndexingResult.IndexingCompleted;
                    }
                }

                if (processedBlocksCount == 0)
                {
                    return SecondPassIndexingResult.NextBlockNotReady;
                }
            }
            finally
            {
                logger.LogInformation("Second-pass indexer has processed the blocks batch {@context}. Processed blocks count: {@processedBlocksCount}", this, processedBlocksCount);
            }

            return SecondPassIndexingResult.IndexingInProgress;
        }
        
        private async Task IndexBlock(BlockHeader blockHeader, IBlockchainDbUnitOfWork unitOfWork)
        {
            if (blockHeader.Number == 2)
            {

            }

            var inputCoins = await unitOfWork.InputCoins.GetByBlock(blockHeader.Id);
            var inputsToSpend = inputCoins
                .Where(x => x.Type == InputCoinType.Regular)
                .ToDictionary(x => x.PreviousOutput);

            var coinsToSpend = await unitOfWork.UnspentCoins.GetAnyOf(inputsToSpend.Keys);

            if (inputsToSpend.Count != coinsToSpend.Count && coinsToSpend.Count != 0)
            {
                throw new InvalidOperationException($"Not all unspent coins found ({coinsToSpend.Count}) for the given inputs to spend ({inputsToSpend.Count})");
            }

            var spentByBlockCoins = coinsToSpend.Select(x => x.Spend(inputsToSpend[x.Id])).ToArray();

            //TODO: insert into xx from select u.* from unspent_coins, input_coins, transaction_headers...
            await unitOfWork.SpentCoins.InsertOrIgnore(spentByBlockCoins);

            var blockOutputCoins = await unitOfWork.UnspentCoins.GetByBlock(blockHeader.Id);

            var balanceUpdates = BalanceUpdatesCalculator.Calculate(
                blockHeader,
                blockOutputCoins,
                spentByBlockCoins);

            await unitOfWork.BalanceUpdates.InsertOrIgnore(balanceUpdates);

            var fees = FeesCalculator.Calculate(
                blockHeader,
                blockOutputCoins,
                spentByBlockCoins);

            await unitOfWork.Fees.InsertOrIgnore(fees);

            // TODO: delete from xx (select u.* from unspent_coins, input_coins, transaction_headers...
            await unitOfWork.UnspentCoins.Remove(spentByBlockCoins.Select(x => x.Id).ToArray());

            NextBlock = blockHeader.Number + 1;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
