using System;
using Indexer.Bilv1.Repositories.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Indexer.Bilv1.Repositories.DesignTime
{
    public class ContextFactory : IDesignTimeDbContextFactory<IndexerBilV1Context>
    {
        public IndexerBilV1Context CreateDbContext(string[] args)
        {
            var connString = Environment.GetEnvironmentVariable("POSTGRE_SQL_CONNECTION_STRING");

            var optionsBuilder = new DbContextOptionsBuilder<IndexerBilV1Context>();
            optionsBuilder.UseNpgsql(connString);

            return new IndexerBilV1Context(optionsBuilder.Options);
        }
    }
}
