using CYarp.Server.Configs;
using CYarp.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Forwarder;

namespace CYarp.Server.Clients
{
    /// <summary>
    /// 客户端
    /// </summary>
    [DebuggerDisplay("Id = {Id}, Protocol = {Protocol}")]
    sealed class Client : IClient
    {
        private int tcpTunnelCount = 0;
        private volatile bool disposed = false;
        private readonly ClientConnection connection;
        private readonly IHttpForwarder httpForwarder;
        private readonly TunnelFactory tunnelFactory;
        private readonly HttpTunnelConfig httpTunnelConfig;
        private readonly HttpContext httpContext;
        private readonly Lazy<HttpMessageInvoker> httpClientLazy;

        private static readonly ForwarderRequestConfig httpRequestConfig = new()
        {
            Version = HttpVersion.Version11,
            VersionPolicy = HttpVersionPolicy.RequestVersionExact
        };

        private static readonly ForwarderRequestConfig httpsRequestConfig = new()
        {
            Version = HttpVersion.Version20,
            VersionPolicy = HttpVersionPolicy.RequestVersionOrLower
        };

        public string Id => this.connection.ClientId;

        public Uri TargetUri { get; }

        public ClaimsPrincipal User => this.httpContext.User;

        public TransportProtocol Protocol => this.httpContext.Features.GetRequiredFeature<ICYarpFeature>().Protocol;

        public int TcpTunnelCount => this.tcpTunnelCount;

        public int HttpTunnelCount => this.connection.HttpTunnelCount;

        public IPEndPoint? RemoteEndpoint
        {
            get
            {
                var connection = this.httpContext.Connection;
                return connection.RemoteIpAddress == null ? null : new IPEndPoint(connection.RemoteIpAddress, connection.RemotePort);
            }
        }

        public DateTimeOffset CreationTime { get; } = DateTimeOffset.Now;


        public Client(
            ClientConnection connection,
            IHttpForwarder httpForwarder,
            TunnelFactory tunnelFactory,
            HttpTunnelConfig httpTunnelConfig,
            Uri clientTargetUri,
            HttpContext httpContext)
        {
            this.connection = connection;
            this.httpForwarder = httpForwarder;
            this.tunnelFactory = tunnelFactory;
            this.httpTunnelConfig = httpTunnelConfig;
            this.TargetUri = clientTargetUri;
            this.httpContext = httpContext;

            this.httpClientLazy = new Lazy<HttpMessageInvoker>(this.CreateHttpClient);
        }

        private HttpMessageInvoker CreateHttpClient()
        {
            var httpHandler = new ClientHttpHandler(this.connection, this.tunnelFactory, this.httpTunnelConfig);
            return new HttpMessageInvoker(httpHandler, disposeHandler: true);
        }

        public async Task<Stream> CreateTcpTunnelAsync(CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(this.disposed, this);

            var tcpTunnel = await this.tunnelFactory.CreateTunnelAsync(this.connection, cancellationToken);
            Interlocked.Increment(ref this.tcpTunnelCount);

            tcpTunnel.DisposingCallback = OnTcpTunnelDisposing;
            return tcpTunnel;

            void OnTcpTunnelDisposing(Tunnel tunnel)
            {
                Interlocked.Decrement(ref this.tcpTunnelCount);
            }
        }


        public ValueTask<ForwarderError> ForwardHttpAsync(HttpContext context, HttpTransformer? transformer)
        {
            ObjectDisposedException.ThrowIf(this.disposed, this);

            var httpClient = this.httpClientLazy.Value;
            var destination = this.TargetUri.OriginalString;
            var requestConfig = this.TargetUri.Scheme == Uri.UriSchemeHttp ? httpRequestConfig : httpsRequestConfig;
            return this.httpForwarder.SendAsync(context, destination, httpClient, requestConfig, transformer ?? HttpTransformer.Default);
        }

        public Task WaitForCloseAsync()
        {
            return this.connection.WaitForCloseAsync();
        }

        public async ValueTask DisposeAsync()
        {
            if (this.disposed == false)
            {
                this.disposed = true;

                if (this.httpClientLazy.IsValueCreated)
                {
                    this.httpClientLazy.Value.Dispose();
                }
                await this.connection.DisposeAsync();
            }
        }


        public override string ToString()
        {
            return this.Id;
        }
    }
}