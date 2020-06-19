using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Domain.Blocks;
using Indexer.Common.Domain.Transactions;
using Indexer.Common.Persistence.Entities.BlockHeaders;
using Indexer.Common.Persistence.Entities.TransactionHeaders;

namespace Indexer.Common.Domain.Indexing.Common
{
    public sealed class PrimaryBlockProcessor
    {
        private readonly IBlockHeadersRepository _blockHeadersRepository;
        private readonly ITransactionHeadersRepository _transactionHeadersRepository;

        public PrimaryBlockProcessor(IBlockHeadersRepository blockHeadersRepository,
            ITransactionHeadersRepository transactionHeadersRepository)
        {
            _blockHeadersRepository = blockHeadersRepository;
            _transactionHeadersRepository = transactionHeadersRepository;
        }

        public async Task Process(BlockHeader blockHeader, IReadOnlyCollection<TransactionHeader> transactionHeaders)
        {
            await _blockHeadersRepository.InsertOrIgnore(blockHeader);
            await _transactionHeadersRepository.InsertOrIgnore(transactionHeaders);
        }
    }
}
