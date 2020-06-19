using System.Linq;
using System.Threading.Tasks;
using Indexer.Common.Domain.Blocks;
using Indexer.Common.Domain.Transactions.Transfers;
using Indexer.Common.Persistence.Entities.InputCoins;
using Indexer.Common.Persistence.Entities.UnspentCoins;

namespace Indexer.Common.Domain.Indexing.Common.CoinBlocks
{
    public sealed class CoinsPrimaryBlockProcessor
    {
        private readonly UnspentCoinsFactory _unspentCoinsFactory;
        private readonly IInputCoinsRepository _inputCoinsRepository;
        private readonly IUnspentCoinsRepository _unspentCoinsRepository;

        public CoinsPrimaryBlockProcessor(UnspentCoinsFactory unspentCoinsFactory,
            IInputCoinsRepository inputCoinsRepository, 
            IUnspentCoinsRepository unspentCoinsRepository)
        {
            _unspentCoinsFactory = unspentCoinsFactory;
            _inputCoinsRepository = inputCoinsRepository;
            _unspentCoinsRepository = unspentCoinsRepository;
        }

        public async Task Process(CoinsBlock block)
        {
            await _inputCoinsRepository.InsertOrIgnore(block.Header.BlockchainId, block.Header.Id, block.Transfers.SelectMany(x => x.InputCoins).ToArray());

            var unspentCoins = await _unspentCoinsFactory.Create(block.Transfers);

            await _unspentCoinsRepository.InsertOrIgnore(block.Header.BlockchainId, unspentCoins);
        }
    }
}
