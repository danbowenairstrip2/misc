using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DynamicHostTest
{
    public class SubWorker : IHostedService
    {
        private readonly string _subWorkerString;
        private readonly ILogger<SubWorker> _logger;
        private CancellationTokenSource _taskCancellationTokenSource;

        public SubWorker(ILogger<SubWorker> logger, string subWorkerString)
        {
            _logger = logger;
            _subWorkerString = subWorkerString;
        }



        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _taskCancellationTokenSource = new CancellationTokenSource();
            _logger.LogInformation("SubWorker {string} starting.", _subWorkerString, DateTimeOffset.Now);

            var task=Task.Run(async () =>
            {
                while (!_taskCancellationTokenSource.IsCancellationRequested || cancellationToken.IsCancellationRequested)
                {
                    _logger.LogDebug("SubWorker {s} running at: {time}", _subWorkerString, DateTimeOffset.Now);
                    await Task.Delay(1000, cancellationToken);
                }
            },_taskCancellationTokenSource.Token);

            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("SubWorker {string} stopping.", _subWorkerString);

            _taskCancellationTokenSource.Cancel();
            
            await Task.CompletedTask;
        }
    }
}