using System;
using System.Threading.Tasks;
using Indexer.Common.Persistence;
using Indexer.Common.ReadModel.Blockchains;
using MassTransit;
using Microsoft.Extensions.Logging;
using Swisschain.Sirius.Integrations.MessagingContract.Blockchains;

namespace Indexer.Worker.MessageConsumers
{
    public class BlockchainUpdatesConsumer : IConsumer<BlockchainAdded>, IConsumer<BlockchainUpdated>
    {
        private readonly ILogger<BlockchainUpdatesConsumer> _logger;
        private readonly IBlockchainsRepository _blockchainsRepository;

        public BlockchainUpdatesConsumer(ILogger<BlockchainUpdatesConsumer> logger, IBlockchainsRepository blockchainsRepository)
        {
            _logger = logger;
            _blockchainsRepository = blockchainsRepository;
        }

        public async Task Consume(ConsumeContext<BlockchainAdded> context)
        {
            var evt = context.Message;

            DateTimeOffset createdAt = evt.CreatedAt;
            await _blockchainsRepository.AddOrReplaceAsync(new Blockchain
            {
                Id = evt.BlockchainId,
                Name = evt.Name,
                NetworkType = evt.NetworkType,
                Protocol = new Common.ReadModel.Blockchains.Protocol
                {
                    Code = evt.Protocol.Code,
                    Name = evt.Protocol.Name,
                    Capabilities = new Common.ReadModel.Blockchains.Capabilities()
                    {
                        DestinationTag = evt.Protocol.Capabilities.DestinationTag == null ? null :
                            new Common.ReadModel.Blockchains.DestinationTagCapabilities()
                            {
                                Text = evt.Protocol.Capabilities.DestinationTag.Text == null ? null :
                            new Common.ReadModel.Blockchains.TextDestinationTagsCapabilities()
                            {
                                MaxLength = evt.Protocol.Capabilities.DestinationTag.Text.MaxLength
                            },
                                Number = evt.Protocol.Capabilities.DestinationTag.Number == null ? null :
                                new Common.ReadModel.Blockchains.NumberDestinationTagsCapabilities()
                                {
                                    Max = evt.Protocol.Capabilities.DestinationTag.Number.Max,
                                    Min = evt.Protocol.Capabilities.DestinationTag.Number.Min
                                }
                            }
                    },
                    DoubleSpendingProtectionType = evt.Protocol.DoubleSpendingProtectionType
                },
                TenantId = evt.TenantId,
                CreatedAt = createdAt,
                UpdatedAt = createdAt,
                StartBlockNumber = evt.StartBlockNumber
            });

            _logger.LogInformation("Blockchain has been added {@context}", evt);
        }

        public async Task Consume(ConsumeContext<BlockchainUpdated> context)
        {
            var evt = context.Message;
            var previous = await _blockchainsRepository.GetAsync(evt.BlockchainId);

            if (previous.UpdatedAt.UtcDateTime < evt.UpdatedAt)
            {
                await _blockchainsRepository.AddOrReplaceAsync(new Blockchain
                {
                    Id = evt.BlockchainId,
                    Name = evt.Name,
                    NetworkType = evt.NetworkType,
                    Protocol = new Common.ReadModel.Blockchains.Protocol
                    {
                        Code = evt.Protocol.Code,
                        Name = evt.Protocol.Name,
                        Capabilities = new Common.ReadModel.Blockchains.Capabilities()
                        {
                            DestinationTag = evt.Protocol.Capabilities.DestinationTag == null ? null :
                            new Common.ReadModel.Blockchains.DestinationTagCapabilities()
                            {
                                Text = evt.Protocol.Capabilities.DestinationTag.Text == null ? null :
                            new Common.ReadModel.Blockchains.TextDestinationTagsCapabilities()
                            {
                                MaxLength = evt.Protocol.Capabilities.DestinationTag.Text.MaxLength
                            },
                                Number = evt.Protocol.Capabilities.DestinationTag.Number == null ? null :
                                new Common.ReadModel.Blockchains.NumberDestinationTagsCapabilities()
                                {
                                    Max = evt.Protocol.Capabilities.DestinationTag.Number.Max,
                                    Min = evt.Protocol.Capabilities.DestinationTag.Number.Min
                                }
                            }
                        },
                        DoubleSpendingProtectionType = evt.Protocol.DoubleSpendingProtectionType,
                    },
                    TenantId = evt.TenantId,
                    CreatedAt = previous.CreatedAt,
                    UpdatedAt = evt.UpdatedAt,
                    StartBlockNumber = evt.StartBlockNumber
                });
            }

            _logger.LogInformation("Blockchain has been updated {@context}", evt);
        }
    }
}
