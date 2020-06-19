using System;
using System.Linq;
using System.Threading.Tasks;
using Indexer.Common.Domain.Blocks;
using Indexer.Common.Persistence.Entities.BalanceUpdates;
using Indexer.Common.Persistence.Entities.BlockHeaders;
using Indexer.Common.Persistence.Entities.Fees;
using Indexer.Common.Persistence.Entities.InputCoins;
using Indexer.Common.Persistence.Entities.SpentCoins;
using Indexer.Common.Persistence.Entities.TransactionHeaders;
using Indexer.Common.Persistence.Entities.UnspentCoins;
using Microsoft.Extensions.Logging;

namespace Indexer.Common.Domain.Indexing.Ongoing
{
    public sealed class CoinsBlockCanceler
    {
        private readonly ILogger<CoinsBlockCanceler> _logger;
        private readonly IBlockHeadersRepository _blockHeadersRepository;
        private readonly ITransactionHeadersRepository _transactionHeadersRepository;
        private readonly IInputCoinsRepository _inputCoinsRepository;
        private readonly IUnspentCoinsRepository _unspentCoinsRepository;
        private readonly ISpentCoinsRepository _spentCoinsRepository;
        private readonly IBalanceUpdatesRepository _balanceUpdatesRepository;
        private readonly IFeesRepository _feesRepository;

        public CoinsBlockCanceler(ILogger<CoinsBlockCanceler> logger,
            IBlockHeadersRepository blockHeadersRepository,
            ITransactionHeadersRepository transactionHeadersRepository,
            IInputCoinsRepository inputCoinsRepository,
            IUnspentCoinsRepository unspentCoinsRepository,
            ISpentCoinsRepository spentCoinsRepository,
            IBalanceUpdatesRepository balanceUpdatesRepository,
            IFeesRepository feesRepository)
        {
            _logger = logger;
            _blockHeadersRepository = blockHeadersRepository;
            _transactionHeadersRepository = transactionHeadersRepository;
            _inputCoinsRepository = inputCoinsRepository;
            _unspentCoinsRepository = unspentCoinsRepository;
            _spentCoinsRepository = spentCoinsRepository;
            _balanceUpdatesRepository = balanceUpdatesRepository;
            _feesRepository = feesRepository;
        }

        public async Task Cancel(BlockHeader blockHeader)
        {
            var lastBlock = await _blockHeadersRepository.GetLast(blockHeader.BlockchainId);

            if (lastBlock.Id != blockHeader.Id)
            {
                _logger.LogError("Can't cancel the block - it's not the last one {@context}", new
                    {
                        BlockchainId = blockHeader.BlockchainId,
                        BlockId = blockHeader.Id,
                        BlockNumber = blockHeader.Number,
                        LastBlockId = lastBlock.Id,
                        LastBlockNumber = lastBlock.Number
                    });

                throw new InvalidOperationException($"Can't cancel the block {blockHeader.BlockchainId}:{blockHeader.Id} ({blockHeader.Number}) - it's not the last one. The last one block is {lastBlock.Id} ({lastBlock.Number})");
            }

            await _blockHeadersRepository.Remove(blockHeader.BlockchainId, blockHeader.Id);
            await _transactionHeadersRepository.RemoveByBlock(blockHeader.BlockchainId, blockHeader.Id);
            await _inputCoinsRepository.RemoveByBlock(blockHeader.BlockchainId, blockHeader.Id);
            await _unspentCoinsRepository.RemoveByBlock(blockHeader.BlockchainId, blockHeader.Id);

            var spentByBlockCoins = await _spentCoinsRepository.GetSpentByBlock(blockHeader.BlockchainId, blockHeader.Id);
            var revertedSpentByBlockCoins = spentByBlockCoins.Select(x => x.Revert()).ToArray();

            await _unspentCoinsRepository.InsertOrIgnore(blockHeader.BlockchainId, revertedSpentByBlockCoins);
            await _spentCoinsRepository.RemoveSpentByBlock(blockHeader.BlockchainId, blockHeader.Id);
            
            await _balanceUpdatesRepository.RemoveByBlock(blockHeader.BlockchainId, blockHeader.Id);
            await _feesRepository.RemoveByBlock(blockHeader.BlockchainId, blockHeader.Id);
        }
    }
}
