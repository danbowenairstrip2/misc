using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DynamicHostTest2
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly SelfHostA _selfHostA;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private CancellationTokenSource _cts;

        public Worker(ILogger<Worker> logger, SelfHostA selfHostA, IHostApplicationLifetime hostApplicationLifetime)
        {
            _logger = logger;
            _selfHostA = selfHostA;
            _hostApplicationLifetime = hostApplicationLifetime;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                stoppingToken.Register(() => _logger.LogDebug("Host stopping token canceled."));

                _cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                _cts.Token.Register(() => _logger.LogDebug("Worker cancellation token canceled."));

                bool isSubWorkerRunning = false;
                int i = 0;
                while (!stoppingToken.IsCancellationRequested)
                {
                    i++;
                    _logger.LogDebug("Worker running at: {time}", DateTimeOffset.Now);
                    await Task.Delay(1500, _cts.Token);
                    if (i % 4 == 0)
                    {
                        if (isSubWorkerRunning)
                        {
                            _logger.LogInformation("Stopping SelfHostA.");
                            await _selfHostA.StopAsync(_cts.Token);
                            isSubWorkerRunning = false;
                        }
                        else
                        {
                            _logger.LogInformation("Starting SelfHostA.");
                            await _selfHostA.StartAsync(_cts.Token);
                            isSubWorkerRunning = true;
                        }
                    }

                    if (i % 15 == 0)
                    {
                        //throwing exception just to see what happens
                        throw new Exception("AllDoneException");
                    }
                }

                _logger.LogInformation("Worker stopped.");

            }
            catch (Exception e)
            {
                _logger.LogError("Worker exception {e}", e.ToString());
                _cts.Cancel();
            }

            _logger.LogInformation("Worker ExecuteAsync done. Disposing SelfHostA.");
            _selfHostA.Dispose();
            
            _logger.LogInformation("Calling StopApplication.");
            _hostApplicationLifetime.StopApplication();

        }
    }
}