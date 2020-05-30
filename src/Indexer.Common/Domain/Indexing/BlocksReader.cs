using System;
using System.Threading.Tasks;
using Indexer.Common.Domain.Blocks;
using Indexer.Common.ReadModel.Blockchains;
using Microsoft.Extensions.Logging;
using Swisschain.Sirius.Sdk.Integrations.Client;
using Swisschain.Sirius.Sdk.Integrations.Contract.Blocks;
using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Domain.Indexing
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

        public async Task<BlockHeader> ReadBlockOrDefaultAsync(long blockNumber)
        {
            var doubleSpendingProtectionType = _blockchainMetamodel.Protocol.DoubleSpendingProtectionType;

            switch (doubleSpendingProtectionType)
            {
                case DoubleSpendingProtectionType.Coins:
                    return await ReadCoinsBlock(blockNumber);

                case DoubleSpendingProtectionType.Nonce:
                default:
                    throw new ArgumentOutOfRangeException(nameof(doubleSpendingProtectionType), doubleSpendingProtectionType, "");
            }
        }

        private async Task<BlockHeader> ReadCoinsBlock(long blockNumber)
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

            return new BlockHeader(
                _blockchainMetamodel.Id,
                response.Block.Base.Id,
                response.Block.Base.Number,
                response.Block.Base.PreviousId,
                response.Block.Base.MinedAt.ToDateTime());
        }
    }
}
