using System;
using System.Threading.Tasks;
using Indexer.Common.Domain.Blocks;
using Indexer.Common.Persistence;
using MassTransit;
using Microsoft.Extensions.Logging;
using Swisschain.Sirius.Indexer.MessagingContract;

namespace Indexer.Common.Domain.Indexing.Ongoing.BlockCancelling
{
    internal sealed class NonceBlockCanceler : IBlockCanceler
    {
        private readonly ILogger<NonceBlockCanceler> _logger;
        private readonly IBlockchainDbUnitOfWorkFactory _blockchainDbUnitOfWorkFactory;
        private readonly IPublishEndpoint _publisher;

        public NonceBlockCanceler(ILogger<NonceBlockCanceler> logger, 
            IBlockchainDbUnitOfWorkFactory blockchainDbUnitOfWorkFactory,
            IPublishEndpoint publisher)
        {
            _logger = logger;
            _blockchainDbUnitOfWorkFactory = blockchainDbUnitOfWorkFactory;
            _publisher = publisher;
        }

        public async Task Cancel(OngoingIndexer indexer, BlockHeader blockHeader)
        {
            await using var unitOfWork = await _blockchainDbUnitOfWorkFactory.StartTransactional(blockHeader.BlockchainId);

            try
            {
                var lastBlock = await unitOfWork.BlockHeaders.GetLast();

                if (lastBlock.Id != blockHeader.Id)
                {
                    _logger.LogError("Can't cancel the block - it's not the last one {@context}",
                        new
                        {
                            BlockchainId = blockHeader.BlockchainId,
                            BlockId = blockHeader.Id,
                            BlockNumber = blockHeader.Number,
                            LastBlockId = lastBlock.Id,
                            LastBlockNumber = lastBlock.Number
                        });

                    throw new InvalidOperationException($"Can't cancel the block {blockHeader.BlockchainId}:{blockHeader.Id} ({blockHeader.Number}) - it's not the last one. The last one block is {lastBlock.Id} ({lastBlock.Number})");
                }

                await CancelBlock(blockHeader, unitOfWork);
                await unitOfWork.Commit();
            }
            catch
            {
                await unitOfWork.Rollback();
                throw;
            }

            await _publisher.Publish(new BlockCancelled
            {
                BlockchainId = indexer.BlockchainId,
                BlockId = blockHeader.Id,
                BlockNumber = blockHeader.Number,
                ChainSequence = indexer.Sequence
            });
        }

        private static async Task CancelBlock(BlockHeader blockHeader, ITransactionalBlockchainDbUnitOfWork unitOfWork)
        {
            await unitOfWork.BlockHeaders.Remove(blockHeader.Id);
            await unitOfWork.TransactionHeaders.RemoveByBlock(blockHeader.Id);
            await unitOfWork.NonceUpdates.RemoveByBlock(blockHeader.Id);
            await unitOfWork.BalanceUpdates.RemoveByBlock(blockHeader.Id);
            await unitOfWork.Fees.RemoveByBlock(blockHeader.Id);
        }
    }
}
