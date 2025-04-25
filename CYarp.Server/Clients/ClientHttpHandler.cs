using CYarp.Server.Configs;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CYarp.Server.Clients
{
    sealed class ClientHttpHandler : DelegatingHandler
    {
        private readonly ClientConnection connection;
        private readonly TunnelFactory tunnelFactory;
        private readonly TunnelStatistics tunnelStatistics;

        public ClientHttpHandler(
            ClientConnection connection,
            TunnelFactory tunnelFactory,
            HttpTunnelConfig httpTunnelConfig,
            TunnelStatistics tunnelStatistics)
        {
            this.connection = connection;
            this.tunnelFactory = tunnelFactory;
            this.tunnelStatistics = tunnelStatistics;
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

            const TunnelType tunnelType = TunnelType.HttpTunnel;
            var httpTunnelCount = this.tunnelStatistics.AddTunnelCount(tunnelType, 1);
            TunnelLog.LogTunnelCreate(this.tunnelFactory.Logger, this.connection.ClientId, httpTunnel.Protocol, httpTunnel.Id, stopwatch.Elapsed, tunnelType, httpTunnelCount);

            httpTunnel.DisposingCallback = OnHttpTunnelDisposing;
            return httpTunnel;


            void OnHttpTunnelDisposing(Tunnel tunnel)
            {
                var httpTunnelCount = this.tunnelStatistics.AddTunnelCount(tunnelType, -1);
                TunnelLog.LogTunnelClosed(this.tunnelFactory.Logger, this.connection.ClientId, tunnel.Protocol, tunnel.Id, tunnel.Lifetime, tunnelType, httpTunnelCount);
            }
        }
    }
}
