using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DynamicHostTest2
{
    public class SelfHostA : IHostedService, IDisposable
    {
        private readonly ILogger<SelfHostA> _logger;
        private CancellationTokenSource _taskCancellationTokenSource;
        private readonly IHost _host;

        public SelfHostA()
        {
            _host = CreateHostBuilder().Build();
            _host.RunAsync();
            _logger = _host.Services.GetRequiredService<ILogger<SelfHostA>>();
        }


        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("SelfHostA  starting host.");

            _taskCancellationTokenSource=new CancellationTokenSource();
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _taskCancellationTokenSource.Token);
            _taskCancellationTokenSource.Token.Register(() => _logger.LogDebug("_taskCancellationTokenSource token cancelled."));

            var m1 = _host.Services.GetRequiredService<Minion>();
            m1.Name = "Bruce";
            _logger.LogDebug($"SelfHostA  created a minion named {m1.Name}");

            var task = Task.Run(async () =>
            {
                while (!_taskCancellationTokenSource.Token.IsCancellationRequested)
                {
                    _logger.LogDebug("SelfHostA is running.");
                    await Task.Delay(1000);
                }
            },_taskCancellationTokenSource.Token).ContinueWith((s)=>_logger.LogDebug("SelfHostA task loop stopped."));

            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("SelfHostA  stopping host.");
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _taskCancellationTokenSource.Token);

            _taskCancellationTokenSource.Cancel();
        }

        public static IHostBuilder CreateHostBuilder() =>
            Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((hostingContext, config) => { config.AddConfiguration(new ConfigurationBuilder().AddJsonFile("appsettings_subworker.json").Build()); })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddTransient<Minion>();

                })
        ;

        public void Dispose()
        {
            _host?.Dispose();
            _taskCancellationTokenSource?.Dispose();
        }
    }
}