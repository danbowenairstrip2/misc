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

        public Worker(ILogger<Worker> logger, SubWorkerFactory subWorkerFactory)
        {
            _logger = logger;
            _subWorkerFactory = subWorkerFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("Worker starting.");
                var subWorker = _subWorkerFactory.GetSubWorker("SubWorker01");
                var subWorker2 = _subWorkerFactory.GetSubWorker("SubWorker02");

                _logger.LogInformation("Starting subWorker.");
                bool isSubWorkerRunning = false;
                bool isSubWorker2Running = true;
                await subWorker.StartAsync(stoppingToken);
                isSubWorkerRunning = true;

                int i = 0;
                while (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogDebug("Worker running at: {time}", DateTimeOffset.Now);
                    await Task.Delay(3000, stoppingToken);
                    if (++i % 4 == 0)
                    {
                        if (isSubWorkerRunning)
                        {
                            _logger.LogInformation("Stopping subWorker.");
                            await subWorker.StopAsync(stoppingToken);
                            isSubWorkerRunning = false;
                        }
                        else
                        {
                            _logger.LogInformation("Restarting subWorker.");
                            await subWorker.StartAsync(stoppingToken);
                            isSubWorkerRunning = true;
                        }

                        if (isSubWorker2Running)
                        {
                            _logger.LogInformation("Stopping subWorker2.");
                            await subWorker2.StopAsync(stoppingToken);
                            isSubWorker2Running = false;
                        }
                        else
                        {
                            _logger.LogInformation("Restarting subWorker2.");
                            await subWorker2.StartAsync(stoppingToken);
                            isSubWorker2Running = true;
                        }
                    }

                    if (++i % 12 == 0)
                    {
                        throw new Exception("AllDoneException");
                    }

                    _logger.LogInformation("Worker stopped.");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                throw;
            }

        }
    }
}