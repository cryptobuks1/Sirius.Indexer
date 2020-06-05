using System;

namespace Indexer.Common.Persistence.Entities.BlockHeaders
{
    public class BlockHeaderEntity
    {
        // ReSharper disable InconsistentNaming
        public string id { get; set; }
        public long number { get; set; }
        public string previous_id { get; set; }
        public DateTime mined_at { get; set; }
    }
}
