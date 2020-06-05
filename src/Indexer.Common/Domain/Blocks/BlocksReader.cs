using System;
using System.Linq;
using System.Threading.Tasks;
using Indexer.Common.Domain.Transactions;
using Indexer.Common.ReadModel.Blockchains;
using Microsoft.Extensions.Logging;
using Swisschain.Sirius.Sdk.Integrations.Client;
using Swisschain.Sirius.Sdk.Integrations.Contract.Blocks;
using Swisschain.Sirius.Sdk.Integrations.Contract.Transactions.Transfers;
using CoinId = Swisschain.Sirius.Sdk.Primitives.CoinId;
using CoinsTransferTransaction = Indexer.Common.Domain.Transactions.Transfers.CoinsTransferTransaction;
using OutputCoin = Indexer.Common.Domain.Transactions.Transfers.OutputCoin;

namespace Indexer.Common.Domain.Blocks
{
    internal class BlocksReader : IBlocksReader
    {
        private readonly ILogger<BlocksReader> _logger;
        private readonly ISiriusIntegrationClient _client;
        private readonly BlockchainMetamodel _blockchainMetamodel;

        public BlocksReader(
            ILogger<BlocksReader> logger,
            ISiriusIntegrationClient client,
            BlockchainMetamodel blockchainMetamodel)
        {
            _logger = logger;
            _client = client;
            _blockchainMetamodel = blockchainMetamodel;
        }

        public async Task<CoinsBlock> ReadCoinsBlockOrDefault(long blockNumber)
        {
            var response = await _client.Blocks.ReadCoinsBlockAsync(new ReadBlockRequest {BlockNumber = blockNumber});

            if (response.KindCase == ReadCoinsBlockResponse.KindOneofCase.Error)
            {
                if (response.Error.Code == ReadBlockError.Types.ErrorCode.BlockNotFound)
                {
                    return null;
                }

                _logger.LogWarning("Failed to read coins block {@context}", new
                {
                    BlockchainId = _blockchainMetamodel.Id,
                    BlockNumber = blockNumber,
                    ErrorCode = response.Error.Code,
                    ErrorMessage = response.Error.Message
                });

                throw new InvalidOperationException($@"Failed to read coins block {blockNumber} from blockchain {_blockchainMetamodel.Id}. Error code: {response.Error.Code}, Error message: {response.Error.Message}");
            }

            var blockHeader = new BlockHeader(
                _blockchainMetamodel.Id,
                response.Block.Header.Id,
                response.Block.Header.Number,
                response.Block.Header.PreviousId,
                response.Block.Header.MinedAt.ToDateTime());

            var transfers = response.Block.Transfers.Select(tx =>
            {
                var txHeader = new TransactionHeader(
                    blockHeader.BlockchainId,
                    blockHeader.Id,
                    tx.Header.Id,
                    tx.Header.Number,
                    tx.Header.Error);

                var inputCoins = tx.InputCoins.Select(x => (CoinId) x).ToArray();
                var outputCoins = tx.OutputCoins
                    .Select(x => new OutputCoin(
                        x.Number,
                        x.Unit,
                        x.Address,
                        x.Tag,
                        DestinationTagTypeMapper.ToDomain(x.TagType)))
                    .ToArray();

                return new CoinsTransferTransaction(
                    txHeader,
                    inputCoins,
                    outputCoins);
            });

            return new CoinsBlock(blockHeader, transfers.ToArray());
        }

        public Task<NonceBlock> ReadNonceBlockOrDefault(long blockNumber)
        {
            throw new NotImplementedException();
        }
    }
}
