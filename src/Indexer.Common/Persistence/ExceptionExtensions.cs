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
            const string primaryKeyNamePrefix = "pk_";
            const string primaryKeyNameSuffix = "_pkey";

            return string.Equals(e.SqlState, constraintViolationErrorCode, StringComparison.InvariantCultureIgnoreCase) &&
                   (e.ConstraintName.StartsWith(primaryKeyNamePrefix, StringComparison.InvariantCultureIgnoreCase) ||
                    e.ConstraintName.EndsWith(primaryKeyNameSuffix, StringComparison.InvariantCultureIgnoreCase));
        }

        public static bool IsUniqueIndexViolationException(this PostgresException e, string indexName)
        {
            const string constraintViolationErrorCode = "23505";

            return string.Equals(e.SqlState, constraintViolationErrorCode, StringComparison.InvariantCultureIgnoreCase) &&
                   string.Equals(e.ConstraintName, indexName, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
