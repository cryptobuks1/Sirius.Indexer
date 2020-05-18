using System;
using GreenPipes;

namespace Indexer.Common.Messaging.DiscardingRateLimiting
{
    public static class DiscardingRateLimitPipeConfiguratorExtensions
    {
        /// <summary>
        /// Specify a discarding rate limit for message processing, so that only the specified number of messages are allowed
        /// per interval. All messages that exceed the limit per interval are discarded.
        /// </summary>
        /// <param name="configurator"></param>
        /// <param name="rateLimit">The number of messages allowed per interval</param>
        /// <param name="interval">The interval</param>
        /// <param name="router">The control pipe used to adjust the rate limit dynamically</param>
        public static void UseDiscardingRateLimit<T>(this IPipeConfigurator<T> configurator, int rateLimit, TimeSpan interval, IPipeRouter router = null)
            where T : class, PipeContext
        {
            if (configurator == null)
            {
                throw new ArgumentNullException(nameof(configurator));
            }

            var specification = new DiscardingRateLimitPipeSpecification<T>(rateLimit, TimeSpan.FromSeconds(1), router);

            configurator.AddPipeSpecification(specification);
        }
    }
}
