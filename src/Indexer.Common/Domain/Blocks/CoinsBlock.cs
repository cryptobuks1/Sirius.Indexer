using System.Collections.Generic;
using Indexer.Common.Domain.Transactions.Transfers;

namespace Indexer.Common.Domain.Blocks
{
    public sealed class CoinsBlock
    {
        public CoinsBlock(BlockHeader header, IReadOnlyCollection<CoinsTransferTransaction> transfers)
        {
            Header = header;
            Transfers = transfers;
        }

        public BlockHeader Header { get; }
        public IReadOnlyCollection<CoinsTransferTransaction> Transfers { get; }
    }
}
