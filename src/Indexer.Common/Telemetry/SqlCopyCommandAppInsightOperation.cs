using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Indexer.Common.Telemetry
{
    public sealed class SqlCopyCommandAppInsightOperation
    {
        private readonly IAppInsight _appInsight;
        private readonly string _entityName;
        private readonly DateTimeOffset _startTime;
        private readonly Stopwatch _stopwatch;

        internal SqlCopyCommandAppInsightOperation(IAppInsight appInsight, string entityName)
        {
            _appInsight = appInsight;
            _entityName = entityName;
            _startTime = DateTimeOffset.UtcNow;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Complete()
        {
            _stopwatch.Stop();

            _appInsight.TrackDependency(
                "SQL",
                $"Bulk Copy {_entityName}",
                $"Bulk Copy {_entityName}",
                _startTime,
                _stopwatch.Elapsed);
        }

        public void Fail(Exception ex)
        {
            _stopwatch.Stop();

            _appInsight.TrackDependencyFailure(
                "SQL",
                $"Bulk Copy {_entityName}",
                $"Bulk Copy {_entityName}",
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
