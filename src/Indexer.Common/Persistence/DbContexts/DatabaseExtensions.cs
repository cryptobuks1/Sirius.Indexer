using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Indexer.Common.Persistence.DbContexts
{
    public static class DatabaseExtensions
    {
        public static async Task<long> GetNextId(this DatabaseContext context, string tableName, string idName)
        {
            await using var cmd = context.Database.GetDbConnection().CreateCommand();

            cmd.CommandText = $"select nextval(pg_get_serial_sequence('{DatabaseContext.SchemaName}.{tableName}', '{idName}'));";

            if (cmd.Connection.State != System.Data.ConnectionState.Open)
            {
                cmd.Connection.Open();
            }

            return (long)cmd.ExecuteScalar();
        }
    }
}
