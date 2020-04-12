using System;
using System.Threading;
using Indexer.Worker.HostedServices;
using Microsoft.Extensions.Logging;

namespace Indexer.Worker.BalanceProcessors
{
    public class BalanceProcessorJob : IDisposable
    {
        private readonly string _blockchainId;
        private readonly ILogger<BalanceProcessorsHost> _logger;
        private readonly BalanceProcessor _balanceProcessor;
        private readonly TimeSpan _delayBetweenBalanceUpdate;
        private readonly Timer _timer;
        private readonly ManualResetEventSlim _done;
        private readonly CancellationTokenSource _cts;

        public BalanceProcessorJob(
            string blockchainId,
            ILogger<BalanceProcessorsHost> logger,
            BalanceProcessor balanceProcessor,
            TimeSpan delayBetweenBalanceUpdate)
        {
            _blockchainId = blockchainId;
            _logger = logger;
            _balanceProcessor = balanceProcessor;
            _delayBetweenBalanceUpdate = delayBetweenBalanceUpdate;

            _timer = new Timer(TimerCallback, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _done = new ManualResetEventSlim(false);
            _cts = new CancellationTokenSource();

            _logger.LogInformation("{blockchain} job is being created.", _blockchainId);
        }

        public void Start()
        {
            _timer.Change(TimeSpan.Zero, Timeout.InfiniteTimeSpan);

            _logger.LogInformation("{blockchain} job is being started.", _blockchainId);
        }

        public void Stop()
        {
            _logger.LogInformation("{blockchain} job is being stopped.", _blockchainId);

            _cts.Cancel();
        }

        public void Wait()
        {
            _done.Wait();
        }

        public void Dispose()
        {
            _timer.Dispose();
            _cts.Dispose();
            _done.Dispose();
        }

        private void TimerCallback(object state)
        {
            _logger.LogInformation("{blockchain} balances processing being started" , _blockchainId);

            try
            {
                _balanceProcessor.ProcessAsync(100).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing balances of {blockchain}", _blockchainId);
            }
            finally
            {
                if (!_cts.IsCancellationRequested)
                {
                    _timer.Change(_delayBetweenBalanceUpdate, Timeout.InfiniteTimeSpan);
                }
            }

            if (_cts.IsCancellationRequested)
            {
                _done.Set();
            }

            _logger.LogInformation("{blockchain} balances processing has been done" , _blockchainId);
        }
    }
}
