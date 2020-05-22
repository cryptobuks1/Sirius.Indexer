using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;

namespace Indexer.Common.Monitoring
{
    internal class AppInsight : IAppInsight
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

            return new AppInsightOperation(_client, _client.StartOperation(telemetry));
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
