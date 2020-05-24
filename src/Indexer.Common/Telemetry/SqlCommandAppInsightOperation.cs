using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;

namespace Indexer.Common.Telemetry
{
    public sealed class SqlCommandAppInsightOperation
    {
        private readonly IAppInsight _appInsight;
        private readonly DbCommand _command;
        private readonly Stopwatch _stopwatch;
        private readonly DateTimeOffset _startTime;

        internal SqlCommandAppInsightOperation(IAppInsight appInsight, DbCommand command)
        {
            _appInsight = appInsight;
            _command = command;
            _startTime = DateTimeOffset.UtcNow;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Complete()
        {
            _stopwatch.Stop();

            _appInsight.TrackDependency(
                "SQL",
                _command.CommandText,
                _command.ToString(),
                _startTime,
                _stopwatch.Elapsed);
        }

        public void Fail(Exception ex)
        {
            _stopwatch.Stop();

            _appInsight.TrackDependencyFailure(
                "SQL",
                _command.CommandText,
                _command.ToString(),
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
