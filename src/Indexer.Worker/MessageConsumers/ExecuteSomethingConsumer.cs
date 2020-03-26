using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;
using Indexer.Common.Domain.AppFeatureExample;

namespace Indexer.Worker.MessageConsumers
{
    // TODO: Just an example
    public class ExecuteSomethingConsumer : IConsumer<ExecuteSomething>
    {
        private readonly ILogger<ExecuteSomethingConsumer> _logger;

        public ExecuteSomethingConsumer(ILogger<ExecuteSomethingConsumer> logger)
        {
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<ExecuteSomething> context)
        {
            var command = context.Message;

            _logger.LogInformation("'Execute something' command has been processed {@command}", command);

            await Task.CompletedTask;
        }
    }
}
