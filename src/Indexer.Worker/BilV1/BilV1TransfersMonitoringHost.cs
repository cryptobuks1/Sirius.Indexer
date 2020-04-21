using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Indexer.Worker.BilV1
{
    public class BilV1TransfersMonitoringHost : IHostedService, IDisposable
    {
        private readonly ILogger<BilV1TransfersMonitoringHost> _logger;
        private readonly BillV1TransfersMonitor _monitor;
        private readonly TimeSpan _monitorPeriod;
        private readonly Timer _timer;
        private readonly ManualResetEventSlim _done;
        private readonly CancellationTokenSource _cts;

        public BilV1TransfersMonitoringHost(ILogger<BilV1TransfersMonitoringHost> logger,
            BillV1TransfersMonitor monitor)
        {
            _logger = logger;
            _monitor = monitor;

            _monitorPeriod = TimeSpan.FromSeconds(10);
            _timer = new Timer(TimerCallback, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _done = new ManualResetEventSlim(false);
            _cts = new CancellationTokenSource();

            _logger.LogInformation("Transfers monitoring host is being created.");
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer.Change(TimeSpan.Zero, Timeout.InfiniteTimeSpan);

            _logger.LogInformation("Transfers monitoring host is being started.");

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Transfers monitoring host is being stopped.");

            _cts.Cancel();
            _done.Wait(cancellationToken);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer.Dispose();
            _cts.Dispose();
            _done.Dispose();
        }

        private void TimerCallback(object state)
        {
            _logger.LogInformation("Transfers monitoring is being started");

            try
            {
                _monitor.ProcessAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to monitor transfers");
            }
            finally
            {
                if (!_cts.IsCancellationRequested)
                {
                    _timer.Change(_monitorPeriod, Timeout.InfiniteTimeSpan);
                }
            }

            if (_cts.IsCancellationRequested)
            {
                _done.Set();
            }

            _logger.LogInformation("Transfers monitoring has been done");
        }
    }
}
