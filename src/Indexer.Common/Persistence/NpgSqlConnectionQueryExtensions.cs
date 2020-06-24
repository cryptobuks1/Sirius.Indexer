using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using MoreLinq.Extensions;
using Npgsql;

namespace Indexer.Common.Persistence
{
    public static class NpgSqlConnectionQueryExtensions
    {
        public const int BatchSize = 1000;

        public static Task<IReadOnlyCollection<TEntity>> QueryInList<TEntity, TSource>(this NpgsqlConnection connection, 
            string schema,
            string table,
            IEnumerable<TSource> source,
            IEnumerable<string> columnsToSelect,
            IEnumerable<string> listColumns,
            Func<TSource, string> listValuesFactory,
            int knownSourceLength = 0,
            bool isListValuesUnique = true)
        {
            return QueryInList<TEntity, TSource>(
                connection,
                schema,
                table,
                source,
                string.Join(", ", columnsToSelect),
                string.Join(", ", listColumns),
                listValuesFactory,
                knownSourceLength,
                isListValuesUnique);
        }

        public static async Task<IReadOnlyCollection<TEntity>> QueryInList<TEntity, TSource>(this NpgsqlConnection connection, 
            string schema,
            string table,
            IEnumerable<TSource> source,
            string columnsToSelect,
            string listColumns,
            Func<TSource, string> listValuesFactory,
            int knownSourceLength = 0,
            bool isListValuesUnique = true)
        {
            async Task<IEnumerable<TEntity>> ReadBatch(IEnumerable<TSource> batch)
            {
                var listKeys = string.Join(", ", batch.Select(listValuesFactory));
                var queryBuilder = new StringBuilder();

                queryBuilder.AppendLine($@"
                    select {columnsToSelect} 
                    from {schema}.{table} 
                    where ({listColumns}) in ({listKeys})");

                if (isListValuesUnique)
                {
                    // limit is specified to avoid scanning indexes of the partitions once all items are found and we know
                    // that list keys are unique
                    queryBuilder.AppendLine("limit @limit");
                }

                var query = queryBuilder.ToString();

                return await connection.QueryAsync<TEntity>(query, new {limit = BatchSize});
            }

            var entities = new List<TEntity>(knownSourceLength == 0 ? BatchSize : knownSourceLength);
            
            foreach (var batch in source.Batch(BatchSize))
            {
                entities.AddRange(await ReadBatch(batch));
            }

            return entities;
        }
    }
}
