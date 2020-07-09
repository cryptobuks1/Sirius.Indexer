using System.Collections.Generic;
using Indexer.Common.Domain.Transactions.Transfers.Nonces;

namespace Indexer.Common.Domain.Blocks
{
    public sealed class NonceBlock
    {
        public NonceBlock(BlockHeader header, IReadOnlyCollection<NonceTransferTransaction> transfers)
        {
            Header = header;
            Transfers = transfers;
        }

        public BlockHeader Header { get; }
        public IReadOnlyCollection<NonceTransferTransaction> Transfers { get; }
    }
}
