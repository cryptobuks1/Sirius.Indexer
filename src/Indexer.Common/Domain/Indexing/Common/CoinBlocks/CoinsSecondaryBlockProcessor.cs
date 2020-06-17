using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Indexer.Common.Domain.Blocks;
using Indexer.Common.Domain.Transactions;
using Indexer.Common.Domain.Transactions.Transfers;
using Indexer.Common.Persistence.Entities.BalanceUpdates;
using Indexer.Common.Persistence.Entities.Fees;
using Indexer.Common.Persistence.Entities.InputCoins;
using Indexer.Common.Persistence.Entities.SpentCoins;
using Indexer.Common.Persistence.Entities.UnspentCoins;
using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Domain.Indexing.Common.CoinBlocks
{
    public class CoinsSecondaryBlockProcessor
    {
        private readonly IInputCoinsRepository _inputCoinsRepository;
        private readonly IUnspentCoinsRepository _unspentCoinsRepository;
        private readonly ISpentCoinsRepository _spentCoinsRepository;
        private readonly IFeesRepository _feesRepository;
        private readonly IBalanceUpdatesRepository _balanceUpdatesRepository;

        public CoinsSecondaryBlockProcessor(IInputCoinsRepository inputCoinsRepository,
            IUnspentCoinsRepository unspentCoinsRepository,
            ISpentCoinsRepository spentCoinsRepository,
            IFeesRepository feesRepository,
            IBalanceUpdatesRepository balanceUpdatesRepository)
        {
            _inputCoinsRepository = inputCoinsRepository;
            _unspentCoinsRepository = unspentCoinsRepository;
            _spentCoinsRepository = spentCoinsRepository;
            _feesRepository = feesRepository;
            _balanceUpdatesRepository = balanceUpdatesRepository;
        }

        public async Task Process(BlockHeader blockHeader)
        {
            var inputCoins = await _inputCoinsRepository.GetByBlock(blockHeader.BlockchainId, blockHeader.Id);
            var inputsToSpend = inputCoins
                .Where(x => x.Type == InputCoinType.Regular)
                .ToDictionary(x => x.PreviousOutput);

            var coinsToSpend = await _unspentCoinsRepository.GetAnyOf(blockHeader.BlockchainId, inputsToSpend.Keys);

            if (inputsToSpend.Count != coinsToSpend.Count && coinsToSpend.Count != 0)
            {
                throw new InvalidOperationException($"Not all unspent coins found {coinsToSpend.Count} for the given inputs to spend {inputsToSpend.Count}");
            }

            if (coinsToSpend.Any())
            {
                var spentByBlockCoins = coinsToSpend.Select(x => x.Spend(inputsToSpend[x.Id])).ToArray();

                await _spentCoinsRepository.InsertOrIgnore(blockHeader.BlockchainId, blockHeader.Id, spentByBlockCoins);

                var blockOutputCoins =
                    await _unspentCoinsRepository.GetByBlock(blockHeader.BlockchainId, blockHeader.Id);

                await UpdateBalances(blockHeader,
                    blockOutputCoins,
                    spentByBlockCoins);
                await UpdateFees(blockHeader,
                    blockOutputCoins,
                    spentByBlockCoins);

                await _unspentCoinsRepository.Remove(blockHeader.BlockchainId,
                    coinsToSpend.Select(x => x.Id).ToArray());
            }
        }

        private async Task UpdateFees(BlockHeader blockHeader,
            IEnumerable<UnspentCoin> blockOutputCoins,
            IEnumerable<SpentCoin> spentByBlockCoins)
        {
            var minted = blockOutputCoins
                .Select(x => new
                {
                    TransactionId = x.Id.TransactionId,
                    AssetId = x.Unit.AssetId,
                    Amount = x.Unit.Amount
                })
                .GroupBy(x => (x.TransactionId, x.AssetId))
                .Select(g => new
                {
                    TransactionId = g.Key.TransactionId,
                    AssetId = g.Key.AssetId,
                    Amount = g.Sum(x => x.Amount)
                });

            var burned = spentByBlockCoins
                .Select(x => new
                {
                    TransactionId = x.SpentByTransactionId,
                    AssetId = x.Unit.AssetId,
                    Amount = x.Unit.Amount
                })
                .GroupBy(x => (x.TransactionId, x.AssetId))
                .Select(g => new
                {
                    TransactionId = g.Key.TransactionId,
                    AssetId = g.Key.AssetId,
                    Amount = g.Sum(x => x.Amount)
                });

            var fees = new Dictionary<(string TransactionId, long AssetId), decimal>();

            foreach (var item in burned)
            {
                fees[(item.TransactionId, item.AssetId)] = item.Amount;
            }

            foreach (var item in minted)
            {
                var key = (item.TransactionId, item.AssetId);

                if (fees.TryGetValue(key, out var currentFee))
                {
                    fees[key] = currentFee - item.Amount;
                }
                else
                {
                    fees.Add(key, -item.Amount);
                }
            }

            foreach (var (feeKey, fee) in fees.ToArray())
            {
                if (fee <= 0)
                {
                    fees.Remove(feeKey);
                }
            }

            await _feesRepository.InsertOrIgnore(
                blockHeader.BlockchainId,
                fees
                    .Select(x => new Fee(
                        x.Key.TransactionId,
                        x.Key.AssetId,
                        blockHeader.Id,
                        x.Value))
                    .ToArray());
        }

        private async Task UpdateBalances(BlockHeader blockHeader,
            IEnumerable<UnspentCoin> blockOutputCoins,
            IEnumerable<SpentCoin> spentByBlockCoins)
        {
            var income = blockOutputCoins
                .Where(x => x.Address != null)
                .Select(x => new
                {
                    Address = x.Address,
                    AssetId = x.Unit.AssetId,
                    Amount = x.Unit.Amount
                })
                .GroupBy(x => (x.Address, x.AssetId))
                .Select(g => new
                {
                    Address = g.Key.Address,
                    AssetId = g.Key.AssetId,
                    Amount = g.Sum(x => x.Amount)
                });

            var outcome = spentByBlockCoins
                .Where(x => x.Address != null)
                .Select(x => new
                {
                    Address = x.Address,
                    AssetId = x.Unit.AssetId,
                    Amount = x.Unit.Amount
                })
                .GroupBy(x => (x.Address, x.AssetId))
                .Select(g => new
                {
                    Address = g.Key.Address,
                    AssetId = g.Key.AssetId,
                    Amount = g.Sum(x => x.Amount)
                });

            var balanceUpdates = new Dictionary<(string Address, long AssetId), decimal>();

            foreach (var item in income)
            {
                balanceUpdates[(item.Address, item.AssetId)] = item.Amount;
            }

            foreach (var item in outcome)
            {
                var key = (item.Address, item.AssetId);

                if (balanceUpdates.TryGetValue(key, out var currentBalanceUpdate))
                {
                    balanceUpdates[key] = currentBalanceUpdate - item.Amount;
                }
                else
                {
                    balanceUpdates.Add(key, -item.Amount);
                }
            }

            foreach (var (balanceUpdateKey, balanceUpdate) in balanceUpdates.ToArray())
            {
                if (balanceUpdate == 0)
                {
                    balanceUpdates.Remove(balanceUpdateKey);
                }
            }

            await _balanceUpdatesRepository.InsertOrIgnore(
                blockHeader.BlockchainId,
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
