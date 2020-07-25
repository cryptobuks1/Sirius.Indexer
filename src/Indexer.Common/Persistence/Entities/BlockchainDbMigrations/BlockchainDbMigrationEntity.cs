using System;

namespace Indexer.Common.Persistence.Entities.BlockchainDbMigrations
{
    internal sealed class BlockchainDbMigrationEntity
    {
        // ReSharper disable InconsistentNaming
        public int version { get; set; }
        public string script { get; set; }
        public DateTime date { get; set; }
    }
}
