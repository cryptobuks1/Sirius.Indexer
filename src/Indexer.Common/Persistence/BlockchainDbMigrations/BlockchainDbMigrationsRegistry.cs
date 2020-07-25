using System.Collections.Generic;
using System.Linq;
using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Persistence.BlockchainDbMigrations
{
    internal sealed class BlockchainDbMigrationsRegistry
    {
        private readonly List<BlockchainDbMigration> _scriptPaths;

        public BlockchainDbMigrationsRegistry()
        {
            _scriptPaths = new List<BlockchainDbMigration>();
        }

        public void Add(MigrationTargetBlockchainType targetBlockchainType, string scriptPath)
        {
            _scriptPaths.Add(new BlockchainDbMigration(_scriptPaths.Count + 1, scriptPath, targetBlockchainType));
        }

        public IReadOnlyCollection<BlockchainDbMigration> GetPending(int currentVersion, 
            DoubleSpendingProtectionType forDoubleSpendingProtectionType)
        {
            return _scriptPaths
                .SkipWhile(x => x.Version <= currentVersion)
                .Where(x => x.IsApplicable(forDoubleSpendingProtectionType))
                .ToArray();
        }
    }
}
