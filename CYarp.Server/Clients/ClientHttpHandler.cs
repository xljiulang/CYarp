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
        private readonly HttpHandlerConfig httpHandlerConfig;
        private readonly TunnelStreamFactory tunnelStreamFactory;
        private readonly IClient client;

        public ClientHttpHandler(
            HttpHandlerConfig httpHandlerConfig,
            TunnelStreamFactory tunnelStreamFactory,
            IClient client)
        {
            this.httpHandlerConfig = httpHandlerConfig;
            this.tunnelStreamFactory = tunnelStreamFactory;
            this.client = client;

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
                MaxConnectionsPerServer = this.httpHandlerConfig.MaxConnectionsPerServer,
                ConnectTimeout = this.httpHandlerConfig.ConnectTimeout,
                ConnectCallback = ConnectAsync,
                AutomaticDecompression = DecompressionMethods.None,
                RequestHeaderEncodingSelector = (header, context) => Encoding.UTF8,
                ResponseHeaderEncodingSelector = (header, context) => Encoding.UTF8,
            };

            if (this.httpHandlerConfig.DangerousAcceptAnyServerCertificate)
            {
                handler.SslOptions.RemoteCertificateValidationCallback = (_, _, _, _) => true;
            }

            return handler;
        }


        private async ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
        {
            return await this.tunnelStreamFactory.CreateAsync(this.client, cancellationToken);
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
