using System.Threading.Tasks;
using Npgsql;

namespace Indexer.Common.Persistence
{
    public class TransactionalBlockchainDbUnitOfWork : BlockchainDbUnitOfWork, ITransactionalBlockchainDbUnitOfWork
    {
        private readonly NpgsqlTransaction _transaction;

        public TransactionalBlockchainDbUnitOfWork(NpgsqlConnection connection, NpgsqlTransaction transaction, string blockchainId) 
            : base(connection, blockchainId)
        {
            _transaction = transaction;
        }

        public Task Commit()
        {
            return _transaction != null 
                ? _transaction.CommitAsync() 
                : Task.CompletedTask;
        }

        public Task Rollback()
        {
            return _transaction != null 
                ? _transaction.RollbackAsync() 
                : Task.CompletedTask;
        }

        public override async ValueTask DisposeAsync()
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
            }

            await base.DisposeAsync();
        }
    }
}
