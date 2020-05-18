using System;
using System.Threading;
using System.Threading.Tasks;
using GreenPipes;
using GreenPipes.Contracts;
using GreenPipes.Internals.Extensions;

namespace Indexer.Common.Messaging.DiscardingRateLimiting
{
    /// <summary>
    /// Limits the number of calls through the filter to a specified count per time interval
    /// specified.
    /// </summary>
    /// <typeparam name="TContext"></typeparam>
    public class DiscardingRateLimitFilter<TContext> :
        IFilter<TContext>,
        IPipe<CommandContext<SetRateLimit>>,
        IDisposable
        where TContext : class, PipeContext
    {
        private readonly TimeSpan _interval;
        private readonly SemaphoreSlim _limit;
        private readonly Timer _timer;
        private int _count;
        private int _rateLimit;

        public DiscardingRateLimitFilter(int rateLimit, TimeSpan interval)
        {
            _rateLimit = rateLimit;
            _interval = interval;
            _limit = new SemaphoreSlim(rateLimit);
            _timer = new Timer(Reset, null, interval, interval);
        }

        public void Dispose()
        {
            _limit?.Dispose();
            _timer?.Dispose();
        }

        void IProbeSite.Probe(ProbeContext context)
        {
            var scope = context.CreateFilterScope("rateLimit");

            scope.Add("limit", _rateLimit);
            scope.Add("available", _limit.CurrentCount);
            scope.Add("interval", _interval);
        }

        public Task Send(TContext context, IPipe<TContext> next)
        {
            var waitAsync = _limit.WaitAsync(TimeSpan.Zero);
            if (waitAsync.IsCompletedSuccessfully())
            {
                if (waitAsync.Result)
                {
                    Interlocked.Increment(ref _count);

                    return next.Send(context);
                }

                return Task.CompletedTask;
            }

            async Task SendAsync()
            {
                if (await waitAsync.ConfigureAwait(false))
                {
                    Interlocked.Increment(ref _count);

                    await next.Send(context).ConfigureAwait(false);
                }
            }

            return SendAsync();
        }

        public async Task Send(CommandContext<SetRateLimit> context)
        {
            var rateLimit = context.Command.RateLimit;
            if (rateLimit < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(rateLimit), "The rate limit must be >= 1");
            }

            var previousLimit = _rateLimit;
            if (rateLimit > previousLimit)
            {
                _limit.Release(rateLimit - previousLimit);
            }
            else
            {
                for (; previousLimit > rateLimit; previousLimit--)
                {
                    await _limit.WaitAsync().ConfigureAwait(false);
                }
            }

            _rateLimit = rateLimit;
        }

        private void Reset(object state)
        {
            var processed = Interlocked.Exchange(ref _count, 0);
            if (processed > 0)
            {
                _limit.Release(processed);
            }
        }
    }
}
