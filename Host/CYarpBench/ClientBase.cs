﻿using CYarp.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CYarpBench
{
    /// <summary>
    /// 客户端后台服务
    /// </summary>
    abstract class ClientBase : BackgroundService
    {
        private readonly string name;
        private readonly IOptionsMonitor<CYarpClientOptions> clientOptions;
        private readonly ILogger logger;

        public ClientBase(
            IOptionsMonitor<CYarpClientOptions> clientOptions,
            ILogger logger)
        {
            this.clientOptions = clientOptions;
            this.logger = logger;
            this.name = this.GetType().Name;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var delay = 3.0d + 3d * Random.Shared.NextDouble();
            await Task.Delay(TimeSpan.FromSeconds(delay), stoppingToken);

            while (stoppingToken.IsCancellationRequested == false)
            {
                try
                {
                    var options = this.clientOptions.Get(this.name);
                    var httpHandler = new SocketsHttpHandler { EnableMultipleHttp2Connections = true };
                    httpHandler.SslOptions.RemoteCertificateValidationCallback = delegate { return true; };
                    using var client = new CYarpClient(options, this.logger, httpHandler);
                    await client.TransportAsync(stoppingToken);

                    this.logger.LogInformation("连接已被关闭，5秒后重新连接");
                    await Task.Delay(TimeSpan.FromSeconds(5d), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (CYarpConnectException ex) when (ex.ErrorCode >= CYarpConnectError.Unauthorized)
                {
                    this.logger.LogError(ex, ex.Message);
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
