using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.WebSockets;
using Npgsql;
using Polly;
using Polly.Retry;

namespace Indexer.Common.Durability
{
    public static class RetryPolicies
    {
        public static AsyncRetryPolicy DefaultRepositoryRetryPolicy()
        {
            return HandlePostgresTimeout().RetryWithExponentialBackOff();
        }

        public static AsyncRetryPolicy DefaultWebServiceRetryPolicy()
        {
            return HandleWebServiceExceptions().RetryWithExponentialBackOff();
        }

        private static PolicyBuilder HandlePostgresTimeout()
        {
            const int connectionTimeoutErrorCode = 110;

            return Policy.Handle<InvalidOperationException>(e =>
                e.InnerException is NpgsqlException npgSqlException &&
                npgSqlException.InnerException is IOException ioException &&
                ioException.InnerException is SocketException socketException &&
                socketException.ErrorCode == connectionTimeoutErrorCode);
        }

        private static PolicyBuilder HandleWebServiceExceptions()
        {
            return Policy
                .Handle<HttpRequestException>()
                .Or<WebException>()
                .Or<WebSocketException>();
        }
    }
}
