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
        private readonly HttpTunnelConfig httpTunnelConfig;
        private readonly TunnelFactory tunnelFactory;
        private readonly ClientConnection connection;

        public ClientHttpHandler(
            HttpTunnelConfig httpTunnelConfig,
            TunnelFactory tunnelFactory,
            ClientConnection connection)
        {
            this.httpTunnelConfig = httpTunnelConfig;
            this.tunnelFactory = tunnelFactory;
            this.connection = connection;

            this.InnerHandler = CreatePrimitiveHandler();
        }

        private SocketsHttpHandler CreatePrimitiveHandler()
        {
            var handler = new SocketsHttpHandler
            {
                Proxy = null,
                UseProxy = false,
                UseCookies = false,
                AllowAutoRedirect = false,
                MaxConnectionsPerServer = this.httpTunnelConfig.MaxTunnelsPerClient,
                ConnectTimeout = this.httpTunnelConfig.CreationTimeout,
                ConnectCallback = ConnectAsync,
                EnableMultipleHttp2Connections = true,
                PooledConnectionLifetime = this.httpTunnelConfig.LifeTime,
                PooledConnectionIdleTimeout = this.httpTunnelConfig.IdleTimeout,
                AutomaticDecompression = DecompressionMethods.None,
                RequestHeaderEncodingSelector = (header, context) => Encoding.UTF8,
                ResponseHeaderEncodingSelector = (header, context) => Encoding.UTF8,
                ActivityHeadersPropagator = this.httpTunnelConfig.ActivityHeadersPropagator,
            };

            if (this.httpTunnelConfig.DangerousAcceptAnyServerCertificate)
            {
                handler.SslOptions.RemoteCertificateValidationCallback = (_, _, _, _) => true;
            }

            return handler;
        }

        private async ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var httpTunnel = await this.tunnelFactory.CreateTunnelAsync(this.connection, cancellationToken);
            stopwatch.Stop();

            var httpTunnelCount = this.connection.IncrementHttpTunnelCount();
            Log.LogTunnelCreate(this.tunnelFactory.Logger, this.connection.ClientId, httpTunnel.Protocol, httpTunnel.Id, stopwatch.Elapsed, httpTunnelCount);

            httpTunnel.DisposeCallback = this.OnHttpTunnelDispose;
            return httpTunnel;
        }

        private void OnHttpTunnelDispose(Tunnel tunnel)
        {
            var httpTunnelCount = this.connection.DecrementHttpTunnelCount();
            Log.LogTunnelClosed(this.tunnelFactory.Logger, this.connection.ClientId, tunnel.Protocol, tunnel.Id, tunnel.Lifetime, httpTunnelCount);
        }


        static partial class Log
        {
            [LoggerMessage(LogLevel.Information, "[{clientId}] 创建了{protocol}协议隧道{tunnelId}，过程耗时{elapsed}，其当前隧道总数为{tunnelCount}")]
            public static partial void LogTunnelCreate(ILogger logger, string clientId, TransportProtocol protocol, TunnelId tunnelId, TimeSpan elapsed, int tunnelCount);

            [LoggerMessage(LogLevel.Information, "[{clientId}] 关闭了{protocol}协议隧道{tunnelId}，生命周期为{lifeTime}，其当前隧道总数为{tunnelCount}")]
            public static partial void LogTunnelClosed(ILogger logger, string? clientId, TransportProtocol protocol, TunnelId tunnelId, TimeSpan lifeTime, int? tunnelCount);
        }
    }
}
