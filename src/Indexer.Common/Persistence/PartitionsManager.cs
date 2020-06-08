using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Indexer.Common.Persistence.Entities.Blockchains;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Indexer.Common.Persistence
{
    internal sealed class PartitionsManager : IPartitionsManager
    {
        private readonly ILogger<PartitionsManager> _logger;
        private readonly Func<Task<NpgsqlConnection>> _connectionFactory;
        
        // This makes Indexer.Worker stateful, but it's intended to work in a single instance anyway and
        // even if several instance will be run nothing bad well happen, just some exceptions possible and retries.
        private readonly ConcurrentDictionary<(string blockchainId, string table), SemaphoreSlim> _tableLocks;

        public PartitionsManager(ILogger<PartitionsManager> logger,
            Func<Task<NpgsqlConnection>> connectionFactory)
        {
            _logger = logger;
            _connectionFactory = connectionFactory;
            _tableLocks = new ConcurrentDictionary<(string blockchainId, string table), SemaphoreSlim>();
        }

        public Task LockPartitionsManagement(string blockchainId, string ofTable, string applicant)
        {
            var schema = BlockchainSchema.Get(blockchainId);

            _logger.LogInformation("Partitions management lock of {@schema}.{@table} is being acquired by {@applicant}...", schema, ofTable, applicant);

            var tableLock = GetTableLock(blockchainId, ofTable);

            try
            {
                return tableLock.WaitAsync();
            }
            finally
            {
                _logger.LogInformation("Partitions management lock of {@schema}.{@table} has been acquired by {@applicant}", schema, ofTable, applicant);
            }
        }

        public void ReleasePartitionsManagement(string blockchainId, string ofTable, string owner)
        {
            var schema = BlockchainSchema.Get(blockchainId);

            _logger.LogInformation("Partitions management lock of {@schema}.{@table} is being released by {@owner}...", schema, ofTable, owner);

            var tableLock = GetTableLock(blockchainId, ofTable);

            tableLock.Release();

            _logger.LogInformation("Partitions management lock of {@schema}.{@table} has been being released by {@owner}...", schema, ofTable, owner);
        }

        public async Task<bool> IsPartitionExists(string blockchainId, 
            string ofTable, 
            long partNumber)
        {
            var schema = BlockchainSchema.Get(blockchainId);
            var table = $"{ofTable}_{partNumber}";

            var query = @"
                select exists 
                (
                    select from pg_catalog.pg_class c
                    join pg_catalog.pg_namespace n ON n.oid = c.relnamespace
                    where 
                        n.nspname = '@schema' and 
                        c.relname = '@table' and
                        c.relkind = 'r'
                )";

            await using var connection = await _connectionFactory.Invoke();

            return await connection.ExecuteScalarAsync<bool>(
                query,
                new
                {
                    schema,
                    table
                });
        }

        public async Task AddPartition(string blockchainId,
            string ofTable,
            string primaryKeyColumn,
            long partNumber,
            long from,
            long to)
        {
            var schema = BlockchainSchema.Get(blockchainId);

            _logger.LogInformation("Partition {@partitionNumber} (@from - @to) is being added to the table {@schema}.{@table}", partNumber, from, to, schema, ofTable);

            var query = $@"
                create table {schema}.{ofTable}_{partNumber} partition of {schema}.{ofTable} for values from ({from}) to ({to});
                alter table {schema}.{ofTable}_{partNumber} add constraint pk_{ofTable}_{partNumber} primary key ({primaryKeyColumn});";

            await using var connection = await _connectionFactory.Invoke();

            await connection.ExecuteAsync(query);

            _logger.LogInformation("Partition {@partitionNumber} (@from - @to) has been added to the table {@schema}.{@table}", partNumber, from, to, schema, ofTable);
        }

        private SemaphoreSlim GetTableLock(string blockchainId, string table)
        {
            return _tableLocks.GetOrAdd((blockchainId, table), key => new SemaphoreSlim(1, 1));
        }
    }
}
