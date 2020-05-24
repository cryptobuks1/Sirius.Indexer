using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Indexer.Common.Persistence.DbContexts
{
    public static class DatabaseExtensions
    {
        public static async Task<long> GetNextId(this DatabaseContext context, string tableName, string idName)
        {
            await using var cmd = context.Database.GetDbConnection().CreateCommand();

            var telemetry = context.AppInsight.StartSqlCommand(cmd);

            try
            {
                cmd.CommandText = $"select nextval(pg_get_serial_sequence('{DatabaseContext.SchemaName}.{tableName}', '{idName}'));";

                if (cmd.Connection.State != System.Data.ConnectionState.Open)
                {
                    cmd.Connection.Open();
                }

                var result = (long) cmd.ExecuteScalar();

                telemetry.Complete();

                return result;
            }
            catch (Exception ex)
            {
                telemetry.Fail(ex);

                throw;
            }
        }
    }
}
