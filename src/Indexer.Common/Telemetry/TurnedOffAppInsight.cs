using System;
using System.Collections.Generic;
using System.Data.Common;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Indexer.Common.Telemetry
{
    internal sealed class TurnedOffAppInsight : IAppInsight
    {
        public void TrackDependency(string type,
            string name,
            string data,
            DateTimeOffset startTime,
            TimeSpan duration,
            IReadOnlyDictionary<string, string> properties = null)
        {
        }

        public void TrackDependencyFailure(string type,
            string name,
            string data,
            DateTimeOffset startTime,
            TimeSpan duration,
            string resultCode,
            IReadOnlyDictionary<string, string> properties = null)
        {
        }

        public void TrackMetric(string name, double value, IReadOnlyDictionary<string, string> properties = null)
        {
        }

        public AppInsightOperation StartRequest(string name, IReadOnlyDictionary<string, string> properties = null)
        {
            return new AppInsightOperation(this, new TurnedOffOperationHolder<RequestTelemetry>(new RequestTelemetry()));
        }

        public void StopOperation(AppInsightOperation operation)
        {
        }

        public SqlCommandAppInsightOperation StartSqlCommand(string query)
        {
            return new SqlCommandAppInsightOperation(this, query);
        }

        public SqlCopyCommandAppInsightOperation StartSqlCopyCommand<TEntity>()
        {
            return new SqlCopyCommandAppInsightOperation(this, string.Empty);
        }

        public void TrackException(Exception exception)
        {
        }

        private class TurnedOffOperationHolder<T> : IOperationHolder<T>
        {
            public TurnedOffOperationHolder(T telemetry)
            {
                Telemetry = telemetry;
            }

            public T Telemetry { get; }

            public void Dispose()
            {
            }
        }
    }
}
