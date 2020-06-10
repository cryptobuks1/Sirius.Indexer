using System;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Indexer.Common.Persistence
{
    internal sealed class DbVersionValidator : IDbVersionValidator
    {
        private readonly ILogger<DbVersionValidator> _logger;
        private readonly Func<Task<NpgsqlConnection>> _connectionFactory;

        public DbVersionValidator(ILogger<DbVersionValidator> logger, 
            Func<Task<NpgsqlConnection>> connectionFactory)
        {
            _logger = logger;
            _connectionFactory = connectionFactory;
        }

        public async Task Validate()
        {
            await using var connection = await _connectionFactory.Invoke();

            _logger.LogInformation("Validating DB version...");

            var version = await connection.ExecuteScalarAsync<string>("select version()");

            if (version.Length <= 14)
            {
                _logger.LogError("Unexpected DB version format: insufficient version string length - '{@version}'", version);

                throw new InvalidOperationException($"Unexpected DB version format: insufficient version string length - '{version}'");
            }

            if (!int.TryParse(version.Substring(11, 2), out var majorVersion))
            {
                _logger.LogError("Unexpected DB version format: can't parse major version as an integer - '{@version}'", version);

                throw new InvalidOperationException($"Unexpected DB version format: can't parse major version as an integer - '{version}'");
            }

            if (majorVersion < 11)
            {
                _logger.LogError("Unsupported DB version: '{@version}'. At least PostgresSQL v11 is required.", version);

                throw new InvalidOperationException($"Unsupported DB version: '{version}'. At least PostgresSQL v11 is required.");
            }

            _logger.LogInformation("DB version is valid '{@version}'", version);
        }
    }
}
