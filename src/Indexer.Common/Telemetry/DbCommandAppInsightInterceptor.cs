using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Indexer.Common.Telemetry
{
    internal sealed class DbCommandAppInsightInterceptor : DbCommandInterceptor
    {
        private readonly IAppInsight _appInsight;

        public DbCommandAppInsightInterceptor(IAppInsight appInsight)
        {
            _appInsight = appInsight;
        }

        public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
        {
            _appInsight.TrackDependency(
                "SQL",
                command.CommandText,
                command.ToString(),
                eventData.StartTime,
                eventData.Duration);

            return base.NonQueryExecuted(command, eventData, result);
        }

        public override Task<int> NonQueryExecutedAsync(DbCommand command,
            CommandExecutedEventData eventData,
            int result,
            CancellationToken cancellationToken = new CancellationToken())
        {
            _appInsight.TrackDependency(
                "SQL",
                command.CommandText,
                command.ToString(),
                eventData.StartTime,
                eventData.Duration);

            return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
        }

        public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
        {
            _appInsight.TrackDependency(
                "SQL",
                command.CommandText,
                command.ToString(),
                eventData.StartTime,
                eventData.Duration);

            return base.ReaderExecuted(command, eventData, result);
        }

        public override Task<DbDataReader> ReaderExecutedAsync(DbCommand command,
            CommandExecutedEventData eventData,
            DbDataReader result,
            CancellationToken cancellationToken = new CancellationToken())
        {
            _appInsight.TrackDependency(
                "SQL",
                command.CommandText,
                command.ToString(),
                eventData.StartTime,
                eventData.Duration);

            return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
        }

        public override object ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object result)
        {
            _appInsight.TrackDependency(
                "SQL",
                command.CommandText,
                command.ToString(),
                eventData.StartTime,
                eventData.Duration);

            return base.ScalarExecuted(command, eventData, result);
        }

        public override Task<object> ScalarExecutedAsync(DbCommand command,
            CommandExecutedEventData eventData,
            object result,
            CancellationToken cancellationToken = new CancellationToken())
        {
            _appInsight.TrackDependency(
                "SQL",
                command.CommandText,
                command.ToString(),
                eventData.StartTime,
                eventData.Duration);

            return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
        }

        public override void CommandFailed(DbCommand command, CommandErrorEventData eventData)
        {
            _appInsight.TrackDependencyFailure(
                "SQL",
                command.CommandText,
                command.ToString(),
                eventData.StartTime,
                eventData.Duration,
                eventData.Exception.Message,
                new Dictionary<string, string>
                {
                    ["exception"] = eventData.Exception.ToString()
                });

            base.CommandFailed(command, eventData);
        }

        public override Task CommandFailedAsync(DbCommand command,
            CommandErrorEventData eventData,
            CancellationToken cancellationToken = new CancellationToken())
        {
            _appInsight.TrackDependencyFailure(
                "SQL",
                command.CommandText,
                command.ToString(),
                eventData.StartTime,
                eventData.Duration,
                eventData.Exception.Message,
                new Dictionary<string, string>
                {
                    ["exception"] = eventData.Exception.ToString()
                });

            return base.CommandFailedAsync(command, eventData, cancellationToken);
        }
    }
}
