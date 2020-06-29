using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Indexer.Common.Persistence.EntityFramework
{
    public class DatabaseContextFactory : IDesignTimeDbContextFactory<CommonDatabaseContext>
    {
        public CommonDatabaseContext CreateDbContext(string[] args)
        {
            var connectionString = Environment.GetEnvironmentVariable("POSTGRE_SQL_CONNECTION_STRING");

            var optionsBuilder = new DbContextOptionsBuilder<CommonDatabaseContext>();
            optionsBuilder.UseNpgsql(connectionString);

            return new CommonDatabaseContext(optionsBuilder.Options, null);
        }
    }
}
