﻿using Swisschain.Sirius.Indexer.ApiClient.Common;
using Swisschain.Sirius.Indexer.ApiContract;

namespace Swisschain.Sirius.Indexer.ApiClient
{
    public class IndexerClient : BaseGrpcClient, IIndexerClient
    {
        public IndexerClient(string serverGrpcUrl) : base(serverGrpcUrl)
        {
            Monitoring = new Monitoring.MonitoringClient(Channel);
            ObservedOperations = new ObservedOperations.ObservedOperationsClient(Channel);
        }

        public Monitoring.MonitoringClient Monitoring { get; }

        public ObservedOperations.ObservedOperationsClient ObservedOperations { get; }
    }
}
