using System;
using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Persistence.BlockchainDbMigrations
{
    internal sealed class BlockchainDbMigration
    {
        public BlockchainDbMigration(int version, string scriptPath, MigrationTargetBlockchainType targetBlockchainType)
        {
            Version = version;
            ScriptPath = scriptPath;
            TargetBlockchainType = targetBlockchainType;
        }

        public int Version { get; }
        public string ScriptPath { get; }
        public MigrationTargetBlockchainType TargetBlockchainType { get; }

        public bool IsApplicable(DoubleSpendingProtectionType forDoubleSpendingProtectionType)
        {
            switch (TargetBlockchainType)
            {
                case MigrationTargetBlockchainType.All:
                    return true;
                case MigrationTargetBlockchainType.Coins:
                    return forDoubleSpendingProtectionType == DoubleSpendingProtectionType.Coins;
                case MigrationTargetBlockchainType.Nonce:
                    return forDoubleSpendingProtectionType == DoubleSpendingProtectionType.Nonce;
                default:
                    throw new ArgumentOutOfRangeException(nameof(TargetBlockchainType), TargetBlockchainType, "");
            }
        }
    }
}
