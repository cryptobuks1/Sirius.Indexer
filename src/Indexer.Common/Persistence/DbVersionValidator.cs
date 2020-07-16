using System;
using System.Threading.Tasks;
using Dapper;
using Indexer.Common.Configuration;
using Microsoft.Extensions.Logging;

namespace Indexer.Common.Persistence
{
    internal sealed class DbVersionValidator : IDbVersionValidator
    {
        private readonly ILogger<DbVersionValidator> _logger;
        private readonly IBlockchainDbConnectionFactory _connectionFactory;
        private readonly AppConfig _config;

        public DbVersionValidator(ILogger<DbVersionValidator> logger, 
            IBlockchainDbConnectionFactory connectionFactory,
            AppConfig config)
        {
            _logger = logger;
            _connectionFactory = connectionFactory;
            _config = config;
        }

        public async Task Validate()
        {
            foreach (var blockchainId in _config.Blockchains.Keys)
            {
                await Validate(blockchainId);
            }
        }

        private async Task Validate(string blockchainId)
        {
            await using var connection = await _connectionFactory.Create(blockchainId);

            _logger.LogInformation("Validating DB version for the blockchain {@blockchainId}...", blockchainId);

            var version = await connection.ExecuteScalarAsync<string>("select version()");

            if (version.Length <= 14)
            {
                _logger.LogError("Unexpected {@blockchainId} DB version format: insufficient version string length - '{@version}'", blockchainId, version);

                throw new InvalidOperationException($"Unexpected {blockchainId} DB version format: insufficient version string length - '{version}'");
            }

            if (!int.TryParse(version.Substring(11, 2), out var majorVersion))
            {
                _logger.LogError("Unexpected {@blockchainId} DB version format: can't parse major version as an integer - '{@version}'", blockchainId, version);

                throw new InvalidOperationException($"Unexpected {blockchainId} DB version format: can't parse major version as an integer - '{version}'");
            }

            if (majorVersion < 11)
            {
                _logger.LogError("Unsupported {@blockchainId} DB version: '{@version}'. At least PostgresSQL v11 is required.", blockchainId, version);

                throw new InvalidOperationException($"Unsupported {blockchainId} DB version: '{version}'. At least PostgresSQL v11 is required.");
            }

            _logger.LogInformation("{@blockchainId} DB version is valid '{@version}'", blockchainId, version);
        }
    }
}
