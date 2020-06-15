using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Indexer.Common.Domain.Blocks;
using Indexer.Common.Domain.Transactions;
using Indexer.Common.Domain.Transactions.Transfers;
using Indexer.Common.Persistence.Entities.BalanceUpdates;
using Indexer.Common.Persistence.Entities.BlockHeaders;
using Indexer.Common.Persistence.Entities.Fees;
using Indexer.Common.Persistence.Entities.InputCoins;
using Indexer.Common.Persistence.Entities.SpentCoins;
using Indexer.Common.Persistence.Entities.UnspentCoins;
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
            IInputCoinsRepository inputCoinsRepository,
            IUnspentCoinsRepository unspentCoinsRepository,
            ISpentCoinsRepository spentCoinsRepository,
            IBalanceUpdatesRepository balanceUpdatesRepository,
            IFeesRepository feesRepository)
        {
            if (IsCompleted)
            {
                return SecondPassIndexingResult.IndexingCompleted;
            }

            var blocks = await blockHeadersRepository.GetBatch(BlockchainId, NextBlock, maxBlocksCount);

            try
            {
                foreach (var block in blocks)
                {
                    if (NextBlock != block.Number)
                    {
                        return SecondPassIndexingResult.IndexingInProgress;
                    }

                    await StepForward(block,
                        appInsight,
                        inputCoinsRepository,
                        unspentCoinsRepository,
                        spentCoinsRepository,
                        balanceUpdatesRepository,
                        feesRepository);

                    if (IsCompleted)
                    {
                        return SecondPassIndexingResult.IndexingCompleted;
                    }
                }
            }
            finally
            {
                logger.LogInformation("Second-pass indexer has processed the blocks batch {@context}", this);
            }

            return SecondPassIndexingResult.IndexingInProgress;
        }
        
        private async Task StepForward(BlockHeader blockHeader,
            IAppInsight appInsight,
            IInputCoinsRepository inputCoinsRepository,
            IUnspentCoinsRepository unspentCoinsRepository,
            ISpentCoinsRepository spentCoinsRepository,
            IBalanceUpdatesRepository balanceUpdatesRepository,
            IFeesRepository feesRepository)
        {
            var telemetry = appInsight.StartRequest("Second-pass block indexing",
                new Dictionary<string, string>
                {
                    ["blockchainId"] = BlockchainId,
                    ["nextBlock"] = NextBlock.ToString()
                });

            try
            {
                var inputCoins = await inputCoinsRepository.GetByBlock(BlockchainId, blockHeader.Id);
                var coinsToSpend = await unspentCoinsRepository.GetAllOf(BlockchainId, inputCoins);

                // TODO: Add tx ID and coin ID, which spent the coins
                var spendCoins = coinsToSpend.Select(x => x.Spend()).ToArray();

                await spentCoinsRepository.InsertOrIgnore(BlockchainId, blockHeader.Id, spendCoins);
                
                var outputCoins = await unspentCoinsRepository.GetByBlock(BlockchainId, blockHeader.Id);

                await UpdateBalances(blockHeader, balanceUpdatesRepository, outputCoins, spendCoins);
                await UpdateFees(blockHeader, feesRepository, outputCoins, spendCoins);

                await unspentCoinsRepository.Remove(BlockchainId, coinsToSpend.Select(x => x.Id).ToArray());

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

        private async Task UpdateFees(BlockHeader blockHeader,
            IFeesRepository feesRepository,
            IReadOnlyCollection<UnspentCoin> outputCoins,
            SpentCoin[] spendCoins)
        {
            var credit = outputCoins
                .Select(x => new
                {
                    TransactionId = x.Id.TransactionId,
                    AssetId = x.Unit.AssetId,
                    Amount = x.Unit.Amount
                })
                .GroupBy(x => (TransactionId: x.TransactionId, AssetId: x.AssetId))
                .Select(g => new
                {
                    TransactionId = g.Key.TransactionId,
                    AssetId = g.Key.AssetId,
                    Amount = g.Sum(x => x.Amount)
                });

            var debit = spendCoins
                .Select(x => new
                {
                    TransactionId = x.Id.TransactionId,
                    AssetId = x.Unit.AssetId,
                    Amount = x.Unit.Amount
                })
                .GroupBy(x => (TransactionId: x.TransactionId, AssetId: x.AssetId))
                .Select(g => new
                {
                    TransactionId = g.Key.TransactionId,
                    AssetId = g.Key.AssetId,
                    Amount = g.Sum(x => x.Amount)
                });

            var fees = new Dictionary<(string TransactionId, long AssetId), decimal>();

            foreach (var item in debit)
            {
                fees[(item.TransactionId, item.AssetId)] = item.Amount;
            }

            foreach (var item in credit)
            {
                fees[(item.TransactionId, item.AssetId)] -= item.Amount;
            }

            await feesRepository.InsertOrIgnore(
                BlockchainId,
                fees
                    .Select(x => new Fee(
                        x.Key.TransactionId,
                        x.Key.AssetId,
                        blockHeader.Id,
                        x.Value))
                    .ToArray());
        }

        private async Task UpdateBalances(BlockHeader blockHeader,
            IBalanceUpdatesRepository balanceUpdatesRepository,
            IReadOnlyCollection<UnspentCoin> outputCoins,
            SpentCoin[] spendCoins)
        {
            var credit = outputCoins
                .Select(x => new
                {
                    Address = x.Address,
                    AssetId = x.Unit.AssetId,
                    Amount = x.Unit.Amount
                })
                .GroupBy(x => (Address: x.Address, AssetId: x.AssetId))
                .Select(g => new
                {
                    Address = g.Key.Address,
                    AssetId = g.Key.AssetId,
                    Amount = g.Sum(x => x.Amount)
                });

            var debit = spendCoins
                .Select(x => new
                {
                    Address = x.Address,
                    AssetId = x.Unit.AssetId,
                    Amount = x.Unit.Amount
                })
                .GroupBy(x => (Address: x.Address, AssetId: x.AssetId))
                .Select(g => new
                {
                    Address = g.Key.Address,
                    AssetId = g.Key.AssetId,
                    Amount = g.Sum(x => x.Amount)
                });

            var balanceUpdates = new Dictionary<(string Address, long AssetId), decimal>();

            foreach (var item in credit)
            {
                balanceUpdates[(item.Address, item.AssetId)] = item.Amount;
            }

            foreach (var item in debit)
            {
                balanceUpdates[(item.Address, item.AssetId)] -= item.Amount;
            }

            await balanceUpdatesRepository.InsertOrIgnore(
                BlockchainId,
                balanceUpdates
                    .Select(x => BalanceUpdate.Create(
                        x.Key.Address,
                        x.Key.AssetId,
                        blockHeader.Number,
                        blockHeader.Id,
                        blockHeader.MinedAt,
                        x.Value))
                    .ToArray());
        }
    }
}
