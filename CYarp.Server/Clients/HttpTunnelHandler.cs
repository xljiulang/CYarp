using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CYarp.Server.Clients
{
    sealed class HttpTunnelHandler : DelegatingHandler
    {
        private readonly TunnelStreamFactory tunnelStreamFactory;

        private readonly IClient client;

        public HttpTunnelHandler(TunnelStreamFactory tunnelStreamFactory, IClient client)
        {
            this.tunnelStreamFactory = tunnelStreamFactory;
            this.client = client;
            InnerHandler = CreatePrimitiveHandler();
        }

        private SocketsHttpHandler CreatePrimitiveHandler()
        {
            return new SocketsHttpHandler
            {
                Proxy = null,
                UseProxy = false,
                UseCookies = false,
                AllowAutoRedirect = false,
                MaxConnectionsPerServer = 5,
                ConnectCallback = ConnectAsync,
                AutomaticDecompression = DecompressionMethods.None,
                RequestHeaderEncodingSelector = (header, context) => Encoding.UTF8,
                ResponseHeaderEncodingSelector = (header, context) => Encoding.UTF8,
            };
        }

        private async ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
        {
            return await tunnelStreamFactory.CreateAsync(client, cancellationToken);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Host = null;
            request.Version = HttpVersion.Version11;
            request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;
            var response = await base.SendAsync(request, cancellationToken);
            return response;
        }
    }
}
