using System;
using Polly;
using Polly.Retry;

namespace Indexer.Common.Durability
{
    public static class PolicyBuilderExtensions
    {
        public static AsyncRetryPolicy RetryWithExponentialBackOff(this PolicyBuilder builder)
        {
            return builder.WaitAndRetryAsync(
                new[]
                {
                    TimeSpan.FromMilliseconds(100), 
                    TimeSpan.FromSeconds(1), 
                    TimeSpan.FromSeconds(4),
                    TimeSpan.FromSeconds(16), 
                    TimeSpan.FromSeconds(32)
                });
        }
    }
}
