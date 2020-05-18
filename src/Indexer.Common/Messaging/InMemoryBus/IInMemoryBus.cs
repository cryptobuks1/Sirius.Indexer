using System.Threading;
using System.Threading.Tasks;

namespace Indexer.Common.Messaging.InMemoryBus
{
    public interface IInMemoryBus
    {
        Task Publish<T>(T evt);
        Task StartAsync(CancellationToken cancellationToken);
        Task StopAsync(CancellationToken cancellationToken);
    }
}
