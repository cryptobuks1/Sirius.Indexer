using System;
using System.Collections.Generic;
using System.Data.Common;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Indexer.Common.Telemetry
{
    public interface IAppInsight
    {
        void TrackDependency(string type,
            string name,
            string data,
            DateTimeOffset startTime,
            TimeSpan duration,
            IReadOnlyDictionary<string, string> properties = null);

        void TrackDependencyFailure(string type,
            string name,
            string data,
            DateTimeOffset startTime,
            TimeSpan duration,
            string resultCode,
            IReadOnlyDictionary<string, string> properties = null);

        void TrackMetric(string name, double value, IReadOnlyDictionary<string, string> properties = null);
        AppInsightOperation StartRequest(string name, IReadOnlyDictionary<string, string> properties = null);
        void StopOperation(AppInsightOperation operation);
        SqlCommandAppInsightOperation StartSqlCommand(DbCommand command);
        SqlCopyCommandAppInsightOperation StartSqlCopyCommand<TEntity>();
        void TrackException(Exception exception);
        
    }
}
