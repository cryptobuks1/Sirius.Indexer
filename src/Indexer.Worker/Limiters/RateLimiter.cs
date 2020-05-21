using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Indexer.Worker.Limiters
{
    internal class RateLimiter : IDisposable
    {
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _limits;
        private readonly SemaphoreSlim _lock;
        private readonly Timer _timer;
        private readonly int _rateLimit;

        public RateLimiter(int rateLimit, TimeSpan interval)
        {
            _rateLimit = rateLimit;
            _limits = new ConcurrentDictionary<string, SemaphoreSlim>();
            _lock = new SemaphoreSlim(1, 1);
            _timer = new Timer(Reset, null, interval, interval);
        }

        public void Dispose()
        {
            _timer?.Dispose();
            
            foreach (var limit in _limits.Values)
            {
                limit.Dispose();
            }

            _lock.Dispose();
        }

        public Task<bool> Wait(string discriminator)
        {
            var limit = GetLimit(discriminator);
            var waitAsync = limit.WaitAsync(TimeSpan.Zero);

            if (waitAsync.IsCompletedSuccessfully)
            {
                if (waitAsync.Result)
                {
                    return Task.FromResult(true);
                }

                return Task.FromResult(false);
            }

            async Task<bool> WaitAsync()
            {
                return await waitAsync.ConfigureAwait(false);
            }

            return WaitAsync();
        }
        
        private void Reset(object state)
        {
            _lock.Wait();

            try
            {
                foreach (var limit in _limits.Values)
                {
                    var processed = _rateLimit - limit.CurrentCount;

                    if (processed > 0)
                    {
                        limit.Release(processed);
                    }
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        private SemaphoreSlim GetLimit(string discriminator)
        {
            if (_limits.TryGetValue(discriminator, out var limit))
            {
                return limit;
            }
            
            _lock.Wait();

            try
            {
                if (!_limits.TryGetValue(discriminator, out limit))
                {
                    limit = new SemaphoreSlim(_rateLimit, _rateLimit);

                    _limits.TryAdd(discriminator, limit);
                }

                return limit;
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}
