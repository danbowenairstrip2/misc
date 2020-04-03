using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DynamicHostTest
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly SubWorkerFactory _subWorkerFactory;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private CancellationTokenSource _cts;

        public Worker(ILogger<Worker> logger, SubWorkerFactory subWorkerFactory, IHostApplicationLifetime hostApplicationLifetime)
        {
            _logger = logger;
            _subWorkerFactory = subWorkerFactory;
            _hostApplicationLifetime = hostApplicationLifetime;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                stoppingToken.Register(() => _logger.LogDebug("Host stopping token canceled."));

                _cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                _cts.Token.Register(() => _logger.LogDebug("Worker cancellation token canceled."));

                _logger.LogInformation("Worker starting.");
                var subWorker = _subWorkerFactory.GetSubWorker("SubWorker01");
                var subWorker2 = _subWorkerFactory.GetSubWorker("SubWorker02");

                _logger.LogInformation("Starting subWorkers.");
                bool isSubWorkerRunning = false;
                bool isSubWorker2Running = true;
                await subWorker.StartAsync(_cts.Token);
                isSubWorkerRunning = true;

                int i = 0;
                while (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogDebug("Worker running at: {time}", DateTimeOffset.Now);
                    await Task.Delay(3000, _cts.Token);
                    if (++i % 4 == 0)
                    {
                        if (isSubWorkerRunning)
                        {
                            _logger.LogInformation("Stopping subWorker1.");
                            await subWorker.StopAsync(_cts.Token);
                            isSubWorkerRunning = false;
                        }
                        else
                        {
                            _logger.LogInformation("Restarting subWorker1.");
                            await subWorker.StartAsync(_cts.Token);
                            isSubWorkerRunning = true;
                        }

                        if (isSubWorker2Running)
                        {
                            _logger.LogInformation("Stopping subWorker2.");
                            await subWorker2.StopAsync(_cts.Token);
                            isSubWorker2Running = false;
                        }
                        else
                        {
                            _logger.LogInformation("Restarting subWorker2.");
                            await subWorker2.StartAsync(_cts.Token);
                            isSubWorker2Running = true;
                        }
                    }

                    if (++i % 12 == 0)
                    {
                        //throwing exception just to see what happens
                        throw new Exception("AllDoneException");
                    }

                    _logger.LogInformation("Worker stopped.");
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Worker exception {e}", e.ToString());
                _cts.Cancel();
//                throw;
            }

            _logger.LogInformation("Worker ExecuteAsync done. Calling StopApplication.");
            _hostApplicationLifetime.StopApplication();
        }
    }
}