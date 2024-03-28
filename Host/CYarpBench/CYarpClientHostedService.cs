using CYarp.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CYarpBench
{
    /// <summary>
    /// Client端和Server端同时集成在同一个进程即可，方便部署
    /// 当然也可以分开在不同的进程中
    /// </summary>
    sealed class CYarpClientHostedService : BackgroundService
    {
        private readonly IOptionsMonitor<CYarpClientOptions> clientOptions;
        private readonly ILogger<CYarpClientHostedService> logger;

        public CYarpClientHostedService(
            IHostApplicationLifetime applicationLifetime,
            IOptionsMonitor<CYarpClientOptions> clientOptions,
            ILogger<CYarpClientHostedService> logger)
        {
            this.clientOptions = clientOptions;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(3d), stoppingToken);
            while (stoppingToken.IsCancellationRequested == false)
            {
                try
                {
                    var options = this.clientOptions.CurrentValue;
                    using var client = new CYarpClient(options);
                    await client.TransportAsync(stoppingToken);

                    this.logger.LogInformation($"传输已被关闭，5秒后重新连接");
                    await Task.Delay(TimeSpan.FromSeconds(5d), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (CYarpConnectException ex) when (ex.ErrorCode == CYarpConnectError.Unauthorized)
                {
                    this.logger.LogError(ex, "身份认证失败");
                    break;
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "连接异常，10秒后重新连接");
                    await Task.Delay(TimeSpan.FromSeconds(10d), stoppingToken);
                }
            }
        }
    }
}
