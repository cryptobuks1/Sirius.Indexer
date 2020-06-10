using System.Threading;
using System.Threading.Tasks;
using Indexer.Common.Persistence;
using Microsoft.Extensions.Hosting;

namespace Indexer.Worker.HostedServices
{
    public class DbVersionValidationHost : IHostedService
    {
        private readonly IDbVersionValidator _dbVersionValidator;

        public DbVersionValidationHost(IDbVersionValidator dbVersionValidator)
        {
            _dbVersionValidator = dbVersionValidator;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return _dbVersionValidator.Validate();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
