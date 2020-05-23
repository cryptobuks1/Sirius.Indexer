using System;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Indexer.Common.Telemetry
{
    public sealed class AppInsightOperation
    {
        private readonly TelemetryClient _client;
        private readonly IOperationHolder<RequestTelemetry> _holder;

        internal AppInsightOperation(TelemetryClient client, IOperationHolder<RequestTelemetry> holder)
        {
            _client = client;
            _holder = holder;
        }

        public void Stop()
        {
            _client.StopOperation(_holder);
        }

        public void TrackException(Exception exception)
        {
            _client.TrackException(exception);
        }

        public void Fail()
        {
            _holder.Telemetry.Success = false;
            Stop();
        }

        public void Fail(Exception ex)
        {
            TrackException(ex);
            Fail();
        }
    }
}
