using System;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Indexer.Common.Persistence
{
    internal static class ExceptionExtensions
    { 
        public static bool IsPrimaryKeyViolationException(this DbUpdateException e)
        {
            return e.InnerException is PostgresException pgEx && pgEx.IsPrimaryKeyViolationException();
        }
        
        public static bool IsPrimaryKeyViolationException(this PostgresException e)
        {
            const string constraintViolationErrorCode = "23505";
            const string primaryKeyNamePrefix = "PK_";

            return string.Equals(e.SqlState, constraintViolationErrorCode, StringComparison.InvariantCultureIgnoreCase)
                   && e.ConstraintName.StartsWith(primaryKeyNamePrefix, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
