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
                    this.logger.LogInformation($"连接已被正常关闭，5秒后重新连接");
                    await Task.Delay(TimeSpan.FromSeconds(5d), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (CYarpClientException ex) when (ex.ErrorCode <= CYarpErrorCode.ConnectUnauthorized)
                {
                    this.logger.LogError(ex, "参数异常");
                    break;
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "连接已被异常关闭，10秒后重新连接");
                    await Task.Delay(TimeSpan.FromSeconds(10d), stoppingToken);
                }
            }
        }
    }
}
