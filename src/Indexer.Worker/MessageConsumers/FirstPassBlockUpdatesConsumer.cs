using System.Threading.Tasks;
using Indexer.Common.Domain.Indexing;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Indexer.Worker.MessageConsumers
{
    public class FirstPassBlockUpdatesConsumer : IConsumer<FirstPassBlockDetected>, IConsumer<FirstPassBlockCancelled>
    {
        private readonly ILogger<FirstPassBlockUpdatesConsumer> _logger;

        public FirstPassBlockUpdatesConsumer(ILogger<FirstPassBlockUpdatesConsumer> logger)
        {
            _logger = logger;
        }

        public Task Consume(ConsumeContext<FirstPassBlockDetected> context)
        {
            var evt = context.Message;

            _logger.LogInformation("First-pass block detected {@context}", evt);

            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<FirstPassBlockCancelled> context)
        {
            throw new System.NotImplementedException();
        }
    }
}
