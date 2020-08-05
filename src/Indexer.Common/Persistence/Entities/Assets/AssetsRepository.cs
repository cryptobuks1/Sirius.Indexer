using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Indexer.Common.Domain.Assets;
using Indexer.Common.Persistence.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Swisschain.Sirius.Sdk.Primitives;
using PostgreSQLCopyHelper;
using Swisschain.Extensions.Postgres;

namespace Indexer.Common.Persistence.Entities.Assets
{
    internal class AssetsRepository : IAssetsRepository
    {
        private readonly Func<CommonDatabaseContext> _contextFactory;

        public AssetsRepository(Func<CommonDatabaseContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<IReadOnlyCollection<Asset>> GetAllAsync(string blockchainId)
        {
            await using var context = _contextFactory.Invoke();

            var entities = await context.Assets
                .Where(x => x.BlockchainId == blockchainId)
                .ToArrayAsync();
            
            return entities.Select(MapToDomain).ToArray();
        }

        public async Task<IReadOnlyCollection<Asset>> GetExisting(string blockchainId, IReadOnlyCollection<BlockchainAssetId> blockchainAssetIds)
        {
            await using var connection = (NpgsqlConnection) _contextFactory.Invoke().Database.GetDbConnection();

            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            var entities = await GetInList(connection, blockchainId, blockchainAssetIds);

            return entities.Select(MapToDomain).ToArray();
        }

        public async Task Add(string blockchainId, IReadOnlyCollection<BlockchainAsset> blockchainAssets)
        {
            if (!blockchainAssets.Any())
            {
                return;
            }

            await using var connection = (NpgsqlConnection) _contextFactory.Invoke().Database.GetDbConnection();

            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            var copyHelper = new PostgreSQLCopyHelper<BlockchainAsset>(CommonDatabaseContext.SchemaName, TableNames.Assets)
                .UsePostgresQuoting()
                .MapVarchar(nameof(AssetEntity.BlockchainId), p => blockchainId)
                .MapVarchar(nameof(AssetEntity.Symbol), p => SqlString.EscapeCopy(p.Id.Symbol))
                .MapVarchar(nameof(AssetEntity.Address), p => p.Id.Address)
                .MapInteger(nameof(AssetEntity.Accuracy), p => p.Accuracy);

            try
            {
                await copyHelper.SaveAllAsync(connection, blockchainAssets);
            }
            catch (PostgresException e) when (e.IsUniqueConstraintViolationException("ix_assets_symbol") ||
                                              e.IsUniqueConstraintViolationException("ix_assets_symbol_address"))
            {
                var notExisting = await ExcludeExistingInDb(connection, blockchainId, blockchainAssets);

                if (notExisting.Any())
                {
                    await copyHelper.SaveAllAsync(connection, notExisting);
                }
            }
        }

        private static async Task<IReadOnlyCollection<BlockchainAsset>> ExcludeExistingInDb(NpgsqlConnection connection, 
            string blockchainId,
            IReadOnlyCollection<BlockchainAsset> blockchainAssets)
        {
            if (!blockchainAssets.Any())
            {
                return Array.Empty<BlockchainAsset>();
            }

            var existingEntities = await GetInList(connection, blockchainId, blockchainAssets.Select(x => x.Id).ToArray());

            var existingIds = existingEntities
                .Select(x => new BlockchainAssetId(x.Symbol, x.Address))
                .ToHashSet();
            
            return blockchainAssets.Where(x => !existingIds.Contains(x.Id)).ToArray();
        }

        private static async Task<IEnumerable<AssetEntity>> GetInList(NpgsqlConnection connection,
            string blockchainId,
            IReadOnlyCollection<BlockchainAssetId> blockchainAssetIds)
        {
            const int batchSize = 1000;

            async Task<IEnumerable<AssetEntity>> ReadBatch(IReadOnlyCollection<BlockchainAssetId> batch)
            {
                var idsWithAddress = batch.Where(x => x.Address != null).ToArray();
                var inListWithAddress = string.Join(", ", idsWithAddress.Select(x => $"('{SqlString.Escape(x.Symbol)}', '{x.Address}')"));
                var idsWithoutAddress = batch.Where(x => x.Address == null).ToArray();
                var inListWithoutAddress = string.Join(", ", idsWithoutAddress.Select(x => $"('{SqlString.Escape(x.Symbol)}')"));

                var queryBuilder = new StringBuilder();

                queryBuilder.AppendLine($"select * from {CommonDatabaseContext.SchemaName}.{TableNames.Assets} where \"BlockchainId\" = @blockchainId and");

                if (idsWithAddress.Any())
                {
                    queryBuilder.AppendLine($"\"Address\" is not null and (\"Symbol\", \"Address\") in (values {inListWithAddress})");

                    if (idsWithoutAddress.Any())
                    {
                        queryBuilder.AppendLine("or");
                    }
                }

                if (idsWithoutAddress.Any())
                {
                    queryBuilder.AppendLine($"\"Address\" is null and \"Symbol\" in (values {inListWithoutAddress})");
                }

                queryBuilder.AppendLine("limit @limit");

                var query = queryBuilder.ToString();

                return await connection.QueryAsync<AssetEntity>(query, new
                {
                    blockchainId = blockchainId,
                    limit = batchSize
                });
            }

            var entities = new List<AssetEntity>(blockchainAssetIds.Count);

            foreach (var batch in MoreLinq.MoreEnumerable.Batch(blockchainAssetIds, batchSize))
            {
                entities.AddRange(await ReadBatch(batch.ToArray()));
            }

            return entities;
        }

        private static Asset MapToDomain(AssetEntity entity)
        {
            return Asset.Restore(
                entity.Id,
                entity.BlockchainId,
                entity.Symbol,
                entity.Address,
                entity.Accuracy);
        }
    }
}
