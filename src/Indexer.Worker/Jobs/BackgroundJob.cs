﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Indexer.Worker.Jobs
{
    internal sealed class BackgroundJob : IDisposable
    {
        private readonly ILogger _logger;
        private readonly string _jobName;
        private readonly Func<object> _jobLoggingContext;
        private readonly Func<Task> _worker;
        private readonly CancellationTokenSource _cts;
        private Task _task;

        public BackgroundJob(ILogger logger,
            string jobName,
            Func<object> jobLoggingContext,
            Func<Task> worker)
        {
            _logger = logger;
            _jobName = jobName;
            _jobLoggingContext = jobLoggingContext;
            _worker = worker;

            _cts = new CancellationTokenSource();

            _logger.LogInformation($"{_jobName} job is being created {{@context}}", _jobLoggingContext.Invoke());
        }

        public bool IsCancellationRequested => _cts.IsCancellationRequested;

        public void Start()
        {
            if (_task != null)
            {
                return;
            }

            _logger.LogInformation($"{_jobName} job is being started {{@context}}", _jobLoggingContext.Invoke());

            _task = Task.Run(DoWork);
        }

        public void Stop()
        {
            if (_cts.IsCancellationRequested)
            {
                return;
            }

            _logger.LogInformation($"{_jobName} job is being stopped {{@context}}", _jobLoggingContext.Invoke());

            _cts.Cancel();
        }

        public async Task Wait()
        {
            if (_task != null)
            {
                await _task;
            }

            _logger.LogInformation($"{_jobName} job has been stopped {{@context}}", _jobLoggingContext.Invoke());
        }

        public void Dispose()
        {
            if (!_cts.IsCancellationRequested)
            {
                Stop();
            }

            if (_task != null && !_task.IsCompleted)
            {
                Wait().GetAwaiter().GetResult();
            }

            _cts.Dispose();
            _task?.Dispose();
        }

        private async Task DoWork()
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    await _worker.Invoke();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error while executing {_jobName} job {{@context}}", _jobLoggingContext.Invoke());
                }
            }
        }
    }
}
