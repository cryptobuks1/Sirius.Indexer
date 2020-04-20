using Swisschain.Sirius.Indexer.ApiContract;

namespace Swisschain.Sirius.Indexer.ApiClient
{
    public interface IIndexerClient
    {
        Monitoring.MonitoringClient Monitoring { get; }

        ObservedOperations.ObservedOperationsClient ObservedOperations { get; }
    }
}
