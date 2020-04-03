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
            _taskCancellationTokenSource.Token.Register(() => _logger.LogDebug("SubWorker {string} taskCancellationToken canceled."));

            _logger.LogInformation("SubWorker {string} starting.", _subWorkerString, DateTimeOffset.Now);

            var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _taskCancellationTokenSource.Token);
            linkedCancellationTokenSource.Token.Register(() => _logger.LogDebug("SubWorker {string} linkkedCancellationToken canceled."));

            var task =Task.Run(async () =>
            {
                try
                {
                    while (!_taskCancellationTokenSource.IsCancellationRequested || cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogDebug("SubWorker {s} running at: {time}", _subWorkerString, DateTimeOffset.Now);
                        await Task.Delay(1000, cancellationToken);
                    }

                    _logger.LogDebug("Subworker {s} task ended.", _subWorkerString);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogDebug("Subworker {s} task canceled.", _subWorkerString);
                }
                catch (Exception e)
                {
                    _logger.LogError("Subworker {s} task exception {e}.", _subWorkerString, e.ToString());
                }
            }, linkedCancellationTokenSource.Token);

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