using System;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Swisschain.Extensions.Postgres;

namespace Indexer.Common.Persistence
{
    internal static class ExceptionExtensions
    { 
        public static bool IsPrimaryKeyViolationException(this DbUpdateException e)
        {
            return e.InnerException is PostgresException pgEx && pgEx.IsPrimaryKeyViolationException();
        }
    }
}
