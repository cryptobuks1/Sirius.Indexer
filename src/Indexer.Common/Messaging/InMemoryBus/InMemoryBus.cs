using System.Threading;
using System.Threading.Tasks;
using MassTransit;

namespace Indexer.Common.Messaging.InMemoryBus
{
    internal sealed class InMemoryBus : IInMemoryBus
    {
        private readonly IBusControl _busControl;

        public InMemoryBus(IBusControl busControl)
        {
            _busControl = busControl;
        }

        public async Task Publish<T>(T evt)
        {
            await _busControl.Publish(evt);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return _busControl.StartAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return _busControl.StopAsync(cancellationToken);
        }
    }
}
