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
        private readonly HttpTunnelFactory httpTunnelFactory;
        private readonly IConnection connection;

        public ClientHttpHandler(
            HttpTunnelConfig httpTunnelConfig,
            HttpTunnelFactory httpTunnelFactory,
            IConnection connection)
        {
            this.httpTunnelConfig = httpTunnelConfig;
            this.httpTunnelFactory = httpTunnelFactory;
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
                MaxConnectionsPerServer = this.httpTunnelConfig.MaxConnectionsPerServer,
                ConnectTimeout = this.httpTunnelConfig.ConnectTimeout,
                ConnectCallback = ConnectAsync,
                AutomaticDecompression = DecompressionMethods.None,
                RequestHeaderEncodingSelector = (header, context) => Encoding.UTF8,
                ResponseHeaderEncodingSelector = (header, context) => Encoding.UTF8,
            };

            if (this.httpTunnelConfig.DangerousAcceptAnyServerCertificate)
            {
                handler.SslOptions.RemoteCertificateValidationCallback = (_, _, _, _) => true;
            }

            return handler;
        }


        private async ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
        {
            return await this.httpTunnelFactory.CreateAsync(this.connection, cancellationToken);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Host = null;
            request.Version = HttpVersion.Version11;
            request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

            return base.SendAsync(request, cancellationToken);
        }
    }
}
