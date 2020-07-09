using System;
using Indexer.Common.Persistence.Entities.BalanceUpdates;
using Indexer.Common.Persistence.Entities.BlockHeaders;
using Indexer.Common.Persistence.Entities.Fees;
using Indexer.Common.Persistence.Entities.InputCoins;
using Indexer.Common.Persistence.Entities.NonceUpdates;
using Indexer.Common.Persistence.Entities.ObservedOperations;
using Indexer.Common.Persistence.Entities.SpentCoins;
using Indexer.Common.Persistence.Entities.TransactionHeaders;
using Indexer.Common.Persistence.Entities.UnspentCoins;

namespace Indexer.Common.Persistence
{
    public interface IBlockchainDbUnitOfWork : IAsyncDisposable
    {
        IObservedOperationsRepository ObservedOperations { get; }
        IBlockHeadersRepository BlockHeaders { get; }
        ITransactionHeadersRepository TransactionHeaders { get; }
        IBalanceUpdatesRepository BalanceUpdates { get; }
        IFeesRepository Fees { get; }

        IUnspentCoinsRepository UnspentCoins { get; }
        ISpentCoinsRepository SpentCoins { get; }
        IInputCoinsRepository InputCoins { get; }

        INonceUpdatesRepository NonceUpdates { get; }
    }
}
