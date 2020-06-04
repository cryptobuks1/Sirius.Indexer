using System;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Indexer.Common.Telemetry
{
    public sealed class AppInsightOperation
    {
        private readonly IAppInsight _appInsight;
        
        internal AppInsightOperation(IAppInsight appInsight, IOperationHolder<RequestTelemetry> holder)
        {
            _appInsight = appInsight;

            Holder = holder;
        }

        public IOperationHolder<RequestTelemetry> Holder { get; }
        public string ResponseCode { get; set; }

        public void Stop(string responseCode = null)
        {
            if (responseCode != null)
            {
                Holder.Telemetry.ResponseCode = responseCode;
            }
            else if (ResponseCode != null)
            {
                Holder.Telemetry.ResponseCode = ResponseCode;
            }
            
            _appInsight.StopOperation(this);
        }

        public void TrackException(Exception exception)
        {
            _appInsight.TrackException(exception);
        }

        public void Fail()
        {
            Holder.Telemetry.Success = false;
            Stop();
        }

        public void Fail(Exception ex)
        {
            TrackException(ex);
            Fail();
        }
    }
}
