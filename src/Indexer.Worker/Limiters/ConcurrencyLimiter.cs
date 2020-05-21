using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Indexer.Worker.Limiters
{
    internal class ConcurrencyLimiter : IDisposable
    {
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _limits;
        private readonly SemaphoreSlim _lock;
        private readonly int _maxConcurrency;

        public ConcurrencyLimiter(int maxConcurrency)
        {
            _maxConcurrency = maxConcurrency;
            _limits = new ConcurrentDictionary<string, SemaphoreSlim>();
            _lock = new SemaphoreSlim(1, 1);
        }

        public void Dispose()
        {
            foreach (var limit in _limits.Values)
            {
                limit.Dispose();
            }

            _lock.Dispose();
        }

        public Task<IDisposable> Enter(string discriminator)
        {
            var limit = GetLimit(discriminator);
            var waitAsync = limit.WaitAsync();

            if (waitAsync.IsCompletedSuccessfully)
            {
                return Task.FromResult<IDisposable>(new Handle(limit));
            }

            async Task<IDisposable> WaitAsync()
            {
                await waitAsync.ConfigureAwait(false);

                return new Handle(limit);
            }

            return WaitAsync();
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
                    limit = new SemaphoreSlim(_maxConcurrency);

                    _limits.TryAdd(discriminator, limit);
                }

                return limit;
            }
            finally
            {
                _lock.Release();
            }
        }

        private sealed class Handle : IDisposable
        {
            private readonly SemaphoreSlim _semaphore;

            public Handle(SemaphoreSlim semaphore)
            {
                _semaphore = semaphore;
            }
            
            public void Dispose()
            {
                _semaphore.Release();
            }
        }
    }
}
