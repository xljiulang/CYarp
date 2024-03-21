using CYarp.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CYarp.Hosting
{
    sealed class CYarpClientHostedService : BackgroundService
    {
        private readonly IOptionsMonitor<CYarpClientOptions> clientOptions;
        private readonly ILogger<CYarpClientHostedService> logger;

        public CYarpClientHostedService(
            IOptionsMonitor<CYarpClientOptions> clientOptions,
            ILogger<CYarpClientHostedService> logger)
        {
            this.clientOptions = clientOptions;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var client = new CYarpClient();
            while (stoppingToken.IsCancellationRequested == false)
            {
                try
                {
                    await client.TransportAsync(this.clientOptions.CurrentValue, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "传输异常");
                    await Task.Delay(TimeSpan.FromSeconds(5d), stoppingToken);
                }
            }
        }
    }
}
