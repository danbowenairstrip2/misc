using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace DynamicHostTest2
{
    public class SelfHostA : IHostedService, IDisposable
    {
        private readonly ILogger<SelfHostA> _logger;
        private CancellationTokenSource _taskCancellationTokenSource=new CancellationTokenSource();
        private readonly IHost _host;

        public SelfHostA()
        {
            _host = CreateHostBuilder().Build();
            _host.RunAsync();
            _logger = _host.Services.GetRequiredService<ILogger<SelfHostA>>();
            _taskCancellationTokenSource.Cancel(); //set initially to cancelled to support reentry guard in StartAsync
        }


        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (!_taskCancellationTokenSource.Token.IsCancellationRequested)
            {
                _logger.LogError("Host is already running.");
                return Task.CompletedTask;
            }

            Task.Delay(100).Wait(); //just to avoid log collisions

            _logger.LogInformation("SelfHostA starting host.");

            //set up and link cancellation token
            _taskCancellationTokenSource=new CancellationTokenSource();
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _taskCancellationTokenSource.Token);
            _taskCancellationTokenSource.Token.Register(() => _logger.LogDebug("_taskCancellationTokenSource token cancelled."));

            //create an instance of a wired up service just to show it works
            var m1 = _host.Services.GetRequiredService<Minion>();
            m1.Name = "Bruce";
            _logger.LogDebug($"SelfHostA  created a minion named {m1.Name}");

            //background task loop. stopped via cancellation token
            Task.Run(async () =>
            {
                while (!_taskCancellationTokenSource.Token.IsCancellationRequested)
                {
                    _logger.LogDebug($"SelfHostA is running at {DateTimeOffset.Now}");
                    await Task.Delay(755, _taskCancellationTokenSource.Token);
                }
            },_taskCancellationTokenSource.Token).ContinueWith((s)=>_logger.LogDebug("SelfHostA task loop stopped."));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Task.Delay(100).Wait(); //just to avoid log collisions

            _logger.LogInformation("SelfHostA  stopping host.");
            _taskCancellationTokenSource.Cancel();

            return Task.CompletedTask;
        }

        //wire up for internal host
        public static IHostBuilder CreateHostBuilder() =>
            Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((hostingContext, config) => { config.AddConfiguration(new ConfigurationBuilder().AddJsonFile("appsettings_subworker.json").Build()); })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole((options)=>options.TimestampFormat="HH:mm:ss.ff "); //set specific timestamp format just to show this is the logger getting used
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddTransient<Minion>();
                })
        ;

        public void Dispose()
        {
            //gotta be sure to dispose of host to ensure graceful shutdown
            _host?.Dispose();
            _taskCancellationTokenSource?.Dispose();
        }
    }
}