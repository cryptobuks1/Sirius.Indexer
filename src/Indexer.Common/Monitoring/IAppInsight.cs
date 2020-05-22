using System;
using System.Collections.Generic;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Indexer.Common.Monitoring
{
    public interface IAppInsight
    {
        void TrackMetric(string name, double value, IReadOnlyDictionary<string, string> properties = null);
        AppInsightOperation StartRequest(string name, IReadOnlyDictionary<string, string> properties = null);
        void TrackException(Exception exception);
    }
}
