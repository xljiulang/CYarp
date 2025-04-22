using CYarp.Server.Configs;
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
            return await this.tunnelFactory.CreateTunnelAsync(this.connection, cancellationToken);
        }
    }
}
