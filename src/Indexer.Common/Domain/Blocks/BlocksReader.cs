using System;
using System.Linq;
using System.Threading.Tasks;
using Indexer.Common.Domain.Transactions;
using Indexer.Common.Domain.Transactions.Transfers.Nonce;
using Indexer.Common.ReadModel.Blockchains;
using Microsoft.Extensions.Logging;
using Swisschain.Sirius.Sdk.Crypto.AddressFormatting;
using Swisschain.Sirius.Sdk.Integrations.Client;
using Swisschain.Sirius.Sdk.Integrations.Contract.Blocks;
using Swisschain.Sirius.Sdk.Integrations.Contract.Transactions.Transfers;
using CoinId = Swisschain.Sirius.Sdk.Primitives.CoinId;
using CoinsTransferTransaction = Indexer.Common.Domain.Transactions.Transfers.Coins.CoinsTransferTransaction;
using OutputCoin = Indexer.Common.Domain.Transactions.Transfers.Coins.OutputCoin;
using InputCoin = Indexer.Common.Domain.Transactions.Transfers.Coins.InputCoin;
using NonceTransferTransaction = Indexer.Common.Domain.Transactions.Transfers.Nonce.NonceTransferTransaction;
using NonceUpdate = Indexer.Common.Domain.Transactions.Transfers.Nonce.NonceUpdate;
using TransferDestination = Indexer.Common.Domain.Transactions.Transfers.Nonce.TransferDestination;
using TransferOperation = Indexer.Common.Domain.Transactions.Transfers.Nonce.TransferOperation;
using TransferSource = Indexer.Common.Domain.Transactions.Transfers.Nonce.TransferSource;
using FeeSource = Swisschain.Sirius.Sdk.Primitives.FeeSource;

namespace Indexer.Common.Domain.Blocks
{
    internal class BlocksReader : IBlocksReader
    {
        private readonly ILogger<BlocksReader> _logger;
        private readonly ISiriusIntegrationClient _client;
        private readonly BlockchainMetamodel _blockchainMetamodel;
        private readonly IAddressFormatter _addressFormatter;

        public BlocksReader(
            ILogger<BlocksReader> logger,
            ISiriusIntegrationClient client,
            BlockchainMetamodel blockchainMetamodel,
            IAddressFormatterFactory addressFormatterFactory)
        {
            _logger = logger;
            _client = client;
            _blockchainMetamodel = blockchainMetamodel;
            _addressFormatter = addressFormatterFactory.Create(_blockchainMetamodel.Protocol.Code);
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

            var blockHeader = MapBlockHeader(response.Block.Header);

            var transfers = response.Block.Transfers.Select(tx =>
            {
                var txHeader = MapTransactionHeader(blockHeader, tx.Header);

                var inputCoins = tx.InputCoins
                    .Select(x => new InputCoin(
                        new CoinId(tx.Header.Id, x.Number),
                        InputCoinTypeMapper.ToDomain(x.Type),
                        x.PreviousOutput))
                    .ToArray();
                var outputCoins = tx.OutputCoins
                    .Select(x => new OutputCoin(
                        x.Number,
                        x.Unit,
                        _addressFormatter.NormalizeOrPassThrough(x.Address, _blockchainMetamodel.NetworkType),
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

        public async Task<NonceBlock> ReadNonceBlockOrDefault(long blockNumber)
        {
            var response = await _client.Blocks.ReadNonceBlockAsync(new ReadBlockRequest {BlockNumber = blockNumber});

            if (response.KindCase == ReadNonceBlockResponse.KindOneofCase.Error)
            {
                if (response.Error.Code == ReadBlockError.Types.ErrorCode.BlockNotFound)
                {
                    return null;
                }

                _logger.LogWarning("Failed to read nonce block {@context}", new
                {
                    BlockchainId = _blockchainMetamodel.Id,
                    BlockNumber = blockNumber,
                    ErrorCode = response.Error.Code,
                    ErrorMessage = response.Error.Message
                });

                throw new InvalidOperationException($@"Failed to read nonce block {blockNumber} from blockchain {_blockchainMetamodel.Id}. Error code: {response.Error.Code}, Error message: {response.Error.Message}");
            }

            var blockHeader = MapBlockHeader(response.Block.Header);

            var transfers = response.Block.Transfers.Select(tx =>
            {
                var txHeader = MapTransactionHeader(blockHeader, tx.Header);

                var operations = tx.Operations
                    .Select(operation => new TransferOperation(
                        operation.Id,
                        operation.Type,
                        operation.Sources
                            .Select(source =>
                            {
                                var address = _addressFormatter.NormalizeOrPassThrough(source.Address, _blockchainMetamodel.NetworkType);

                                return address != null
                                    ? new TransferSource(
                                        new Sender(address),
                                        source.Unit)
                                    : null;
                            })
                            .Where(x => x != null)
                            .ToArray(),
                        operation.Destinations
                            .Select(destination =>
                            {
                                var address = _addressFormatter.NormalizeOrPassThrough(destination.Address, _blockchainMetamodel.NetworkType);

                                return address != null
                                    ? new TransferDestination(
                                        new Recipient(
                                            address,
                                            destination.Tag,
                                            DestinationTagTypeMapper.ToDomain(destination.TagType)),
                                        destination.Unit)
                                    : null;
                            })
                            .Where(x => x != null)
                            .ToArray()))
                    .ToArray();

                var nonceUpdates = tx.NonceUpdates
                    .Select(nonceUpdate =>
                    {
                        var address = _addressFormatter.NormalizeOrPassThrough(nonceUpdate.Address, _blockchainMetamodel.NetworkType);

                        return address != null 
                            ? new NonceUpdate(address, blockHeader.Id, nonceUpdate.Nonce)
                            : null;
                    })
                    .Where(x => x != null)
                    .ToArray();

                var fees = tx.Fees
                    .Select(feeSource =>
                    {
                        var address = _addressFormatter.NormalizeOrPassThrough(feeSource.FeePayer, _blockchainMetamodel.NetworkType);

                        return address != null
                            ? new FeeSource(address, feeSource.Fees)
                            : null;

                    })
                    .Where(x => x != null)
                    .ToArray();

                return new NonceTransferTransaction(
                    txHeader,
                    operations,
                    nonceUpdates,
                    fees);
            });

            return new NonceBlock(blockHeader, transfers.ToArray());
        }

        private BlockHeader MapBlockHeader(Swisschain.Sirius.Sdk.Integrations.Contract.Blocks.BlockHeader blockHeader)
        {
            return new BlockHeader(
                _blockchainMetamodel.Id,
                blockHeader.Id,
                blockHeader.Number,
                blockHeader.PreviousId,
                blockHeader.MinedAt.ToDateTime());
        }

        private static TransactionHeader MapTransactionHeader(BlockHeader blockHeader, 
            Swisschain.Sirius.Sdk.Integrations.Contract.Transactions.TransactionHeader transactionHeader)
        {
            var txHeader = TransactionHeader.Create(
                blockHeader.BlockchainId,
                blockHeader.Id,
                transactionHeader.Id,
                transactionHeader.Number,
                transactionHeader.Error);
            return txHeader;
        }
    }
}
