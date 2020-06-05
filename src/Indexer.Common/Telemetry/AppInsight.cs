using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;

namespace Indexer.Common.Telemetry
{
    internal sealed class AppInsight : IAppInsight
    {
        private readonly IDictionary<string, string> _defaultProperties;
        private readonly TelemetryClient _client;

        public AppInsight(AppInsightOptions options)
        {
            var module = new DependencyTrackingTelemetryModule();
            module.IncludeDiagnosticSourceActivities.Add("MassTransit");

            var configuration = TelemetryConfiguration.CreateDefault();
            configuration.InstrumentationKey = options.InstrumentationKey;
            configuration.TelemetryInitializers.Add(new HttpDependenciesParsingTelemetryInitializer());
            configuration.TelemetryInitializers.Add(new OperationCorrelationTelemetryInitializer());

            _client = new TelemetryClient(configuration);

            module.Initialize(configuration);

            _defaultProperties = options.DefaultProperties;
        }

        public void TrackMetric(string name, double value, IReadOnlyDictionary<string, string> properties = null)
        {
            var effectiveProperties = GetEffectiveProperties(properties);

            _client.TrackMetric(name, value, effectiveProperties);
        }

        public void TrackDependency(string type,
            string name,
            string data,
            DateTimeOffset startTime,
            TimeSpan duration,
            IReadOnlyDictionary<string, string> properties = null)
        {
            var telemetry = new DependencyTelemetry(
                type,
                null,
                name,
                data,
                startTime,
                duration,
                null,
                true);

            var effectiveProperties = GetEffectiveProperties(properties);

            if (effectiveProperties != null)
            {
                foreach (var (key, value) in effectiveProperties)
                {
                    telemetry.Properties[key] = value;
                }
            }

            _client.TrackDependency(telemetry);
        }

        public void TrackDependencyFailure(string type,
            string name,
            string data,
            DateTimeOffset startTime,
            TimeSpan duration,
            string resultCode,
            IReadOnlyDictionary<string, string> properties = null)
        {
            var telemetry = new DependencyTelemetry(type,
                null,
                name,
                data,
                startTime,
                duration,
                resultCode,
                false);

            var effectiveProperties = GetEffectiveProperties(properties);

            if (effectiveProperties != null)
            {
                foreach (var (key, value) in effectiveProperties)
                {
                    telemetry.Properties[key] = value;
                }
            }

            _client.TrackDependency(telemetry);
        }

        public AppInsightOperation StartRequest(string name, IReadOnlyDictionary<string, string> properties = null)
        {
            var telemetry = new RequestTelemetry {Name = name};
            var effectiveProperties = GetEffectiveProperties(properties);

            if (effectiveProperties != null)
            {
                foreach (var (key, value) in effectiveProperties)
                {
                    telemetry.Properties[key] = value;
                }
            }

            return new AppInsightOperation(this, _client.StartOperation(telemetry));
        }

        public void StopOperation(AppInsightOperation operation)
        {
            _client.StopOperation(operation.Holder);
        }

        public SqlCommandAppInsightOperation StartSqlCommand(string query)
        {
            return new SqlCommandAppInsightOperation(this, query);
        }

        public SqlCopyCommandAppInsightOperation StartSqlCopyCommand<TEntity>()
        {
            return new SqlCopyCommandAppInsightOperation(this, typeof(TEntity).Name);
        }

        public void TrackException(Exception exception)
        {
            _client.TrackException(exception);
        }

        private IDictionary<string, string> GetEffectiveProperties(IReadOnlyDictionary<string, string> properties)
        {
            if (properties != null)
            {
                if (_defaultProperties != null)
                {
                    var effectiveProperties = new Dictionary<string, string>();

                    foreach (var (key, value) in _defaultProperties)
                    {
                        effectiveProperties[key] = value;
                    }

                    foreach (var (key, value) in properties)
                    {
                        effectiveProperties[key] = value;
                    }

                    return effectiveProperties;
                }

                return properties.ToDictionary(x => x.Key, x => x.Value);
            }

            return _defaultProperties;
        }
    }
}
