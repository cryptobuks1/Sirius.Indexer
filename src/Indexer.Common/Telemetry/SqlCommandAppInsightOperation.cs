using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Indexer.Common.Telemetry
{
    public sealed class SqlCommandAppInsightOperation
    {
        private readonly IAppInsight _appInsight;
        private readonly string _query;
        private readonly Stopwatch _stopwatch;
        private readonly DateTimeOffset _startTime;

        internal SqlCommandAppInsightOperation(IAppInsight appInsight, string query)
        {
            _appInsight = appInsight;
            _query = query;
            _startTime = DateTimeOffset.UtcNow;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Complete()
        {
            _stopwatch.Stop();

            _appInsight.TrackDependency(
                "SQL",
                _query,
                _query,
                _startTime,
                _stopwatch.Elapsed);
        }

        public void Fail(Exception ex)
        {
            _stopwatch.Stop();

            _appInsight.TrackDependencyFailure(
                "SQL",
                _query,
                _query,
                _startTime,
                _stopwatch.Elapsed,
                ex.Message,
                new Dictionary<string, string>
                {
                    ["exception"] = ex.ToString()
                });
        }
    }
}
