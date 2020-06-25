using System;

namespace Indexer.Common.Persistence.Entities.BalanceUpdates
{
    internal sealed class BalanceUpdateEntity
    {
        // ReSharper disable InconsistentNaming
        public string address { get; set; }
        public long asset_id { get; set; }
        public long block_number { get; set; }
        public string block_id { get; set; }
        public DateTime block_mined_at { get; set; }
        public decimal amount { get; set; }
    }
}
