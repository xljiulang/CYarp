using CYarp.Server.Configs;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CYarp.Server.Clients
{
    sealed partial class ClientHttpHandler : DelegatingHandler
    {
        private readonly ClientConnection connection;
        private readonly TunnelFactory tunnelFactory;
        private readonly ClientStatistics clientStatistics;

        public ClientHttpHandler(
            ClientConnection connection,
            TunnelFactory tunnelFactory,
            HttpTunnelConfig httpTunnelConfig,
            ClientStatistics clientStatistics)
        {
            this.connection = connection;
            this.tunnelFactory = tunnelFactory;
            this.clientStatistics = clientStatistics;
            this.InnerHandler = CreatePrimitiveHandler(httpTunnelConfig);
        }

        private SocketsHttpHandler CreatePrimitiveHandler(HttpTunnelConfig httpTunnelConfig)
        {
            var handler = new SocketsHttpHandler
            {
                Proxy = null,
                UseProxy = false,
                UseCookies = false,
                AllowAutoRedirect = false,
                MaxConnectionsPerServer = httpTunnelConfig.MaxTunnelsPerClient,
                ConnectTimeout = httpTunnelConfig.CreationTimeout,
                ConnectCallback = CreateHttpTunnelAsync,
                EnableMultipleHttp2Connections = true,
                PooledConnectionLifetime = httpTunnelConfig.LifeTime,
                PooledConnectionIdleTimeout = httpTunnelConfig.IdleTimeout,
                AutomaticDecompression = DecompressionMethods.None,
                RequestHeaderEncodingSelector = (header, context) => Encoding.UTF8,
                ResponseHeaderEncodingSelector = (header, context) => Encoding.UTF8,
                ActivityHeadersPropagator = httpTunnelConfig.ActivityHeadersPropagator,
            };

            if (httpTunnelConfig.DangerousAcceptAnyServerCertificate)
            {
                handler.SslOptions.RemoteCertificateValidationCallback = (_, _, _, _) => true;
            }

            return handler;
        }

        private async ValueTask<Stream> CreateHttpTunnelAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var httpTunnel = await this.tunnelFactory.CreateTunnelAsync(this.connection, cancellationToken);
            stopwatch.Stop();

            var httpTunnelCount = this.clientStatistics.AddHttpTunnelCount(1);
            Log.LogTunnelCreate(this.tunnelFactory.Logger, this.connection.ClientId, httpTunnel.Protocol, httpTunnel.Id, stopwatch.Elapsed, httpTunnelCount);

            httpTunnel.DisposingCallback = this.OnHttpTunnelDisposing;
            return httpTunnel;
        }

        private void OnHttpTunnelDisposing(Tunnel tunnel)
        {
            var httpTunnelCount = this.clientStatistics.AddHttpTunnelCount(-1);
            Log.LogTunnelClosed(this.tunnelFactory.Logger, this.connection.ClientId, tunnel.Protocol, tunnel.Id, tunnel.Lifetime, httpTunnelCount);
        }


        static partial class Log
        {
            [LoggerMessage(LogLevel.Information, "[{clientId}] 创建了{protocol}协议隧道{tunnelId}，过程耗时{elapsed}，其当前http隧道总数为{tunnelCount}")]
            public static partial void LogTunnelCreate(ILogger logger, string clientId, TransportProtocol protocol, TunnelId tunnelId, TimeSpan elapsed, int tunnelCount);

            [LoggerMessage(LogLevel.Information, "[{clientId}] 关闭了{protocol}协议隧道{tunnelId}，生命周期为{lifeTime}，其当前http隧道总数为{tunnelCount}")]
            public static partial void LogTunnelClosed(ILogger logger, string? clientId, TransportProtocol protocol, TunnelId tunnelId, TimeSpan lifeTime, int? tunnelCount);
        }
    }
}
