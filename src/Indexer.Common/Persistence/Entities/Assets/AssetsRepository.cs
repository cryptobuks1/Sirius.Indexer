using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Indexer.Common.Domain.Assets;
using Npgsql;
using Swisschain.Sirius.Sdk.Primitives;
using PostgreSQLCopyHelper;

namespace Indexer.Common.Persistence.Entities.Assets
{
    internal class AssetsRepository : IAssetsRepository
    {
        private readonly Func<Task<NpgsqlConnection>> _connectionFactory;

        public AssetsRepository(Func<Task<NpgsqlConnection>> connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IReadOnlyCollection<Asset>> GetAllAsync(string blockchainId)
        {
            await using var connection = await _connectionFactory.Invoke();

            var schema = DbSchema.GetName(blockchainId);
            var query = $"select * from {schema}.{TableNames.Assets}";
            var entities = await connection.QueryAsync<AssetEntity>(query);

            return entities.Select(x => MapToDomain(blockchainId, x)).ToArray();
        }

        public async Task<IReadOnlyCollection<Asset>> GetExisting(string blockchainId, IReadOnlyCollection<BlockchainAssetId> blockchainAssetIds)
        {
            await using var connection = await _connectionFactory.Invoke();

            var schema = DbSchema.GetName(blockchainId);
            var entities = await GetInList(connection, schema, blockchainAssetIds);

            return entities.Select(x => MapToDomain(blockchainId, x)).ToArray();
        }

        public async Task Add(string blockchainId, IReadOnlyCollection<BlockchainAsset> blockchainAssets)
        {
            if (!blockchainAssets.Any())
            {
                return;
            }

            await using var connection = await _connectionFactory.Invoke();

            var schema = DbSchema.GetName(blockchainId);

            var copyHelper = new PostgreSQLCopyHelper<BlockchainAsset>(schema, TableNames.Assets)
                .UsePostgresQuoting()
                .MapVarchar(nameof(AssetEntity.symbol), p => p.Id.Symbol)
                .MapVarchar(nameof(AssetEntity.address), p => p.Id.Address)
                .MapInteger(nameof(AssetEntity.accuracy), p => p.Accuracy);

            try
            {
                await copyHelper.SaveAllAsync(connection, blockchainAssets);
            }
            catch (PostgresException e) when (e.IsUniqueIndexViolationException("ix_assets_symbol") ||
                                              e.IsUniqueIndexViolationException("ix_assets_symbol_address"))
            {
                var notExisting = await ExcludeExistingInDb(connection, schema, blockchainAssets);

                if (notExisting.Any())
                {
                    await copyHelper.SaveAllAsync(connection, notExisting);
                }
            }
        }

        private static async Task<IReadOnlyCollection<BlockchainAsset>> ExcludeExistingInDb(NpgsqlConnection connection, 
            string schema,
            IReadOnlyCollection<BlockchainAsset> blockchainAssets)
        {
            if (!blockchainAssets.Any())
            {
                return Array.Empty<BlockchainAsset>();
            }

            var existingEntities = await GetInList(connection, schema, blockchainAssets.Select(x => x.Id).ToArray());

            var existingIds = Enumerable.ToHashSet(existingEntities
                    .Select(x => new BlockchainAssetId(x.symbol, x.address)));
            
            return blockchainAssets.Where(x => !existingIds.Contains(x.Id)).ToArray();
        }

        private static async Task<IEnumerable<AssetEntity>> GetInList(NpgsqlConnection connection,
            string schema, 
            IReadOnlyCollection<BlockchainAssetId> blockchainAssetIds)
        {
            const int batchSize = 1000;

            async Task<IEnumerable<AssetEntity>> ReadBatch(IReadOnlyCollection<BlockchainAssetId> batch)
            {
                var idsWithAddress = batch.Where(x => x.Address != null).ToArray();
                var inListWithAddress = string.Join(", ", idsWithAddress.Select(x => $"('{x.Symbol}', '{x.Address}'"));
                var idsWithoutAddress = batch.Where(x => x.Address == null).ToArray();
                var inListWithoutAddress = string.Join("', '", idsWithoutAddress.Select(x => x.Symbol));

                var queryBuilder = new StringBuilder();

                queryBuilder.AppendLine($"select * from {schema}.{TableNames.Assets} where");

                if (idsWithAddress.Any())
                {
                    queryBuilder.AppendLine($"address is not null and (symbol, address) in ({inListWithAddress})");

                    if (idsWithoutAddress.Any())
                    {
                        queryBuilder.AppendLine("or");
                    }
                }

                if (idsWithoutAddress.Any())
                {
                    queryBuilder.AppendLine($"address is null and symbol in ('{inListWithoutAddress}')");
                }

                queryBuilder.AppendLine("limit @limit");

                var query = queryBuilder.ToString();

                return await connection.QueryAsync<AssetEntity>(query, new {limit = batchSize});
            }

            var entities = new List<AssetEntity>(blockchainAssetIds.Count);

            foreach (var batch in MoreLinq.MoreEnumerable.Batch(blockchainAssetIds, batchSize))
            {
                entities.AddRange(await ReadBatch(batch.ToArray()));
            }

            return entities;
        }

        private static Asset MapToDomain(string blockchainId, AssetEntity entity)
        {
            return Asset.Restore(
                entity.id,
                blockchainId,
                entity.symbol,
                entity.address,
                entity.accuracy);
        }
    }
}
