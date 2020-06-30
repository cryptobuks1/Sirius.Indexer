using System.Threading.Tasks;
using Indexer.Common.Persistence;
using Indexer.Common.Persistence.Entities.BalanceUpdates;
using Indexer.Common.Persistence.Entities.BlockHeaders;
using Indexer.Common.Persistence.Entities.Fees;
using Indexer.Common.Persistence.Entities.InputCoins;
using Indexer.Common.Persistence.Entities.ObservedOperations;
using Indexer.Common.Persistence.Entities.SpentCoins;
using Indexer.Common.Persistence.Entities.TransactionHeaders;
using Indexer.Common.Persistence.Entities.UnspentCoins;

namespace IndexerTests.Sdk.Mocks
{
    public class TestBlockchainDbUnitOfWork : IBlockchainDbUnitOfWork
    {
        public IObservedOperationsRepository ObservedOperations { get; set; }
        public IBlockHeadersRepository BlockHeaders { get; set; } = new InMemoryBlockHeadersRepository();
        public ITransactionHeadersRepository TransactionHeaders { get; set; }
        public IUnspentCoinsRepository UnspentCoins { get; set; }
        public ISpentCoinsRepository SpentCoins { get; set; }
        public IInputCoinsRepository InputCoins { get; set; }
        public IBalanceUpdatesRepository BalanceUpdates { get; set; }
        public IFeesRepository Fees { get; set; }

        public ValueTask DisposeAsync()
        {
            return new ValueTask(Task.CompletedTask);
        }
    }
}