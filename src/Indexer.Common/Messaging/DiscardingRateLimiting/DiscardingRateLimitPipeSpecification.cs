using System;
using System.Collections.Generic;
using GreenPipes;

namespace Indexer.Common.Messaging.DiscardingRateLimiting
{
    public class DiscardingRateLimitPipeSpecification<T> :
        IPipeSpecification<T>
        where T : class, PipeContext
    {
        private readonly TimeSpan _interval;
        private readonly int _rateLimit;
        private readonly IPipeRouter _router;

        public DiscardingRateLimitPipeSpecification(int rateLimit, TimeSpan interval, IPipeRouter router = null)
        {
            _rateLimit = rateLimit;
            _interval = interval;
            _router = router;
        }

        public void Apply(IPipeBuilder<T> builder)
        {
            var filter = new DiscardingRateLimitFilter<T>(_rateLimit, _interval);

            builder.AddFilter(filter);

            _router?.ConnectPipe(filter);
        }

        public IEnumerable<ValidationResult> Validate()
        {
            if (_rateLimit < 1)
            {
                yield return this.Failure("RateLimit", "must be >= 1");
            }

            if (_interval <= TimeSpan.Zero)
            {
                yield return this.Failure("Interval", "must be > 0");
            }
        }
    }
}