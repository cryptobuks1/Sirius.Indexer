using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Indexer.Worker.MessageConsumers
{
    //public class PublishAssetsConsumer : IConsumer<ExecuteSomething>
    //{
    //    private readonly ILogger<ExecuteSomethingConsumer> _logger;

    //    public PublishAssetsConsumer(ILogger<ExecuteSomethingConsumer> logger)
    //    {
    //        _logger = logger;
    //    }

    //    public async Task Consume(ConsumeContext<ExecuteSomething> context)
    //    {
    //        var command = context.Message;

    //        _logger.LogInformation("'Execute something' command has been processed {@command}", command);

    //        await Task.CompletedTask;
    //    }
    //}
}
