using Swisschain.Sirius.Indexer.ApiContract.Monitoring;
using Swisschain.Sirius.Indexer.ApiContract.Nonces;
using Swisschain.Sirius.Indexer.ApiContract.ObservedOperations;
using Swisschain.Sirius.Indexer.ApiContract.UnspentCoins;

namespace Swisschain.Sirius.Indexer.ApiClient
{
    public interface IIndexerClient
    {
        Monitoring.MonitoringClient Monitoring { get; }
        ObservedOperations.ObservedOperationsClient ObservedOperations { get; }
        UnspentCoins.UnspentCoinsClient UnspentCoins { get; }
        Nonces.NoncesClient Nonces { get; }
        
    }
}
