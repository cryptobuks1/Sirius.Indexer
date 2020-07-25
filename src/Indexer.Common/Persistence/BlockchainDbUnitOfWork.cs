using System;
using System.Threading.Tasks;
using Indexer.Common.Persistence.Entities.BalanceUpdates;
using Indexer.Common.Persistence.Entities.BlockHeaders;
using Indexer.Common.Persistence.Entities.Fees;
using Indexer.Common.Persistence.Entities.InputCoins;
using Indexer.Common.Persistence.Entities.NonceUpdates;
using Indexer.Common.Persistence.Entities.ObservedOperations;
using Indexer.Common.Persistence.Entities.SpentCoins;
using Indexer.Common.Persistence.Entities.TransactionHeaders;
using Indexer.Common.Persistence.Entities.UnspentCoins;
using Npgsql;

namespace Indexer.Common.Persistence
{
    public class BlockchainDbUnitOfWork : IBlockchainDbUnitOfWork
    {
        private readonly NpgsqlConnection _connection;
        
        private readonly Lazy<IObservedOperationsRepository> _observedOperations;
        private readonly Lazy<IBlockHeadersRepository> _blockHeaders;
        private readonly Lazy<ITransactionHeadersRepository> _transactionHeaders;
        private readonly Lazy<IBalanceUpdatesRepository> _balanceUpdates;
        private readonly Lazy<IFeesRepository> _fees;
        
        private readonly Lazy<IUnspentCoinsRepository> _unspentCoins;
        private readonly Lazy<ISpentCoinsRepository> _spentCoins;
        private readonly Lazy<IInputCoinsRepository> _inputCoins;
        
        private readonly Lazy<INonceUpdatesRepository> _nonceUpdates;

        public BlockchainDbUnitOfWork(NpgsqlConnection connection, string blockchainId)
        {
            _connection = connection;
            
            var schema = DbSchema.GetName(blockchainId);

            _observedOperations = new Lazy<IObservedOperationsRepository>(() =>
                new ObservedOperationsRepositoryRetryDecorator(
                    new ObservedOperationsRepository(connection, schema, blockchainId)));
            _blockHeaders = new Lazy<IBlockHeadersRepository>(() =>
                new BlockHeadersRepositoryRetryDecorator(
                    new BlockHeadersRepository(connection, schema, blockchainId)));
            _transactionHeaders = new Lazy<ITransactionHeadersRepository>(() =>
                new TransactionHeadersRepositoryRetryDecorator(
                    new TransactionHeadersRepository(connection, schema)));
            _balanceUpdates = new Lazy<IBalanceUpdatesRepository>(() =>
                new BalanceUpdatesRepositoryRetryDecorator(
                    new BalanceUpdatesRepository(connection, schema)));
            _fees = new Lazy<IFeesRepository>(() =>
                new FeesRepositoryRetryDecorator(
                    new FeesRepository(connection, schema)));

            _unspentCoins = new Lazy<IUnspentCoinsRepository>(() =>
                new UnspentCoinsRepositoryRetryDecorator(
                    new UnspentCoinsRepository(connection, schema)));
            _spentCoins = new Lazy<ISpentCoinsRepository>(() =>
                new SpentCoinsRepositoryRetryDecorator(
                    new SpentCoinsRepository(connection, schema)));
            _inputCoins = new Lazy<IInputCoinsRepository>(() =>
                new InputCoinsRepositoryRetryDecorator(
                    new InputCoinsRepository(connection, schema)));

            _nonceUpdates = new Lazy<INonceUpdatesRepository>(() =>
                new NonceUpdatesRepository(connection, schema));
        }

        public IObservedOperationsRepository ObservedOperations => _observedOperations.Value;
        public IBlockHeadersRepository BlockHeaders => _blockHeaders.Value;
        public ITransactionHeadersRepository TransactionHeaders => _transactionHeaders.Value;
        public IBalanceUpdatesRepository BalanceUpdates => _balanceUpdates.Value;
        public IFeesRepository Fees => _fees.Value;

        public IUnspentCoinsRepository UnspentCoins => _unspentCoins.Value;
        public ISpentCoinsRepository SpentCoins => _spentCoins.Value;
        public IInputCoinsRepository InputCoins => _inputCoins.Value;

        public INonceUpdatesRepository NonceUpdates => _nonceUpdates.Value;

        public virtual async ValueTask DisposeAsync()
        {
            if (_connection != null)
            {
                await _connection.DisposeAsync();
            }
        }
    }
}
