using System;
using System.Collections.Generic;
using System.Data.Common;

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

        SqlCommandAppInsightOperation StartSqlCommand(DbCommand command);
        
        void TrackException(Exception exception);
    }
}
