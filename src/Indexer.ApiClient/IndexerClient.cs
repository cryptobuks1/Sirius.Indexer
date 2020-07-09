using Swisschain.Sirius.Indexer.ApiClient.Common;
using Swisschain.Sirius.Indexer.ApiContract.Monitoring;
using Swisschain.Sirius.Indexer.ApiContract.Nonces;
using Swisschain.Sirius.Indexer.ApiContract.ObservedOperations;
using Swisschain.Sirius.Indexer.ApiContract.UnspentCoins;

namespace Swisschain.Sirius.Indexer.ApiClient
{
    public class IndexerClient : BaseGrpcClient, IIndexerClient
    {
        public IndexerClient(string serverGrpcUrl) : base(serverGrpcUrl)
        {
            Monitoring = new Monitoring.MonitoringClient(Channel);
            ObservedOperations = new ObservedOperations.ObservedOperationsClient(Channel);
            UnspentCoins = new UnspentCoins.UnspentCoinsClient(Channel);
            Nonces = new Nonces.NoncesClient(Channel);
        }

        public Monitoring.MonitoringClient Monitoring { get; }
        public ObservedOperations.ObservedOperationsClient ObservedOperations { get; }
        public UnspentCoins.UnspentCoinsClient UnspentCoins { get; }
        public Nonces.NoncesClient Nonces { get; }
    }
}
