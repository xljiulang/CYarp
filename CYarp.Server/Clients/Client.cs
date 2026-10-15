using CYarp.Server.Configs;
using Microsoft.AspNetCore.Http;
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
    /// Client
    /// </summary>
    [DebuggerDisplay("Id = {Id}, Protocol = {Protocol}")]
    sealed class Client : IClient
    {
        private volatile bool disposed = false;
        private readonly ClientConnection connection;
        private readonly IHttpForwarder httpForwarder;
        private readonly TunnelFactory tunnelFactory;
        private readonly ClientHttpHandler httpHandler;
        private readonly NonDisposingHandler nonDisposingHandler;
        private readonly TunnelStatistics tunnelStatistics = new();
        private readonly ForwarderRequestConfig requestConfig;

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


        public Uri TargetUri { get; }

        public ClaimsPrincipal User { get; }

        public TransportProtocol Protocol { get; }

        public IPEndPoint? RemoteEndpoint { get; set; }

        public string Id => this.connection.ClientId;

        public int TcpTunnelCount => this.tunnelStatistics.TcpTunnelCount;

        public int HttpTunnelCount => this.tunnelStatistics.HttpTunnelCount;

        public DateTimeOffset CreationTime { get; } = DateTimeOffset.Now;

        public Client(
            ClientConnection connection,
            IHttpForwarder httpForwarder,
            TunnelFactory tunnelFactory,
            HttpTunnelConfig httpTunnelConfig,
            Uri targetUri,
            TransportProtocol protocol,
            ClaimsPrincipal user)
        {
            this.connection = connection;
            this.httpForwarder = httpForwarder;
            this.tunnelFactory = tunnelFactory;

            this.TargetUri = targetUri;
            this.Protocol = protocol;
            this.User = user;

            this.requestConfig = targetUri.Scheme == Uri.UriSchemeHttp ? httpRequestConfig : httpsRequestConfig;
            this.httpHandler = new ClientHttpHandler(this.connection, this.tunnelFactory, httpTunnelConfig, this.tunnelStatistics);
            this.nonDisposingHandler = new NonDisposingHandler(this.httpHandler);
        }

        public async Task<Stream> CreateTcpTunnelAsync(CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(this.disposed, this);

            var stopwatch = Stopwatch.StartNew();
            const TunnelType tunnelType = TunnelType.TcpTunnel;
            var tcpTunnel = await this.tunnelFactory.CreateTunnelAsync(this.connection, tunnelType, cancellationToken);
            stopwatch.Stop();

            var tcpTunnelCount = this.tunnelStatistics.AddTunnelCount(tunnelType, 1);
            TunnelLog.LogTunnelCreate(this.tunnelFactory.Logger, this.connection.ClientId, tcpTunnel.Protocol, tcpTunnel.Id, stopwatch.Elapsed, tunnelType, tcpTunnelCount);

            tcpTunnel.DisposingCallback = OnTcpTunnelDisposing;
            return tcpTunnel;

            void OnTcpTunnelDisposing(Tunnel tunnel)
            {
                var tcpTunnelCount = this.tunnelStatistics.AddTunnelCount(tunnelType, -1);
                TunnelLog.LogTunnelClosed(this.tunnelFactory.Logger, this.connection.ClientId, tunnel.Protocol, tunnel.Id, tunnel.Lifetime, tunnelType, tcpTunnelCount);
            }
        }

        public ValueTask<ForwarderError> ForwardHttpAsync(HttpContext context, HttpTransformer? transformer)
        {
            ObjectDisposedException.ThrowIf(this.disposed, this);

            var httpClient = new HttpMessageInvoker(this.httpHandler, disposeHandler: false);
            var destination = this.TargetUri.OriginalString;
            return this.httpForwarder.SendAsync(context, destination, httpClient, this.requestConfig, transformer ?? HttpTransformer.Default);
        }

        public HttpClient CreateHttpClient(DelegatingHandler? handler = default)
        {
            ObjectDisposedException.ThrowIf(this.disposed, this);

            if (handler == null)
            {
                return this.CreateHttpClientCore(this.httpHandler, disposeHandler: false);
            }

            if (handler.InnerHandler != null)
            {
                throw new ArgumentException($"The {nameof(handler.InnerHandler)} of the {nameof(handler)} must be null", nameof(handler));
            }

            handler.InnerHandler = this.nonDisposingHandler;
            return this.CreateHttpClientCore(handler, disposeHandler: true);
        }

        private HttpClient CreateHttpClientCore(HttpMessageHandler handler, bool disposeHandler)
        {
            var requestConfig = this.requestConfig;
            var httpClient = new HttpClient(handler, disposeHandler)
            {
                BaseAddress = this.TargetUri
            };

            if (requestConfig.Version != null)
            {
                httpClient.DefaultRequestVersion = requestConfig.Version;
            }

            if (requestConfig.VersionPolicy != null)
            {
                httpClient.DefaultVersionPolicy = requestConfig.VersionPolicy.Value;
            }

            return httpClient;
        }

        public Task<ClientCloseReason> WaitForCloseAsync()
        {
            return this.connection.WaitForCloseAsync();
        }

        public async ValueTask DisposeAsync()
        {
            if (this.disposed == false)
            {
                this.disposed = true;

                this.httpHandler.Dispose();
                await this.connection.DisposeAsync();
            }
        }


        public override string ToString()
        {
            return this.Id;
        }


        /// <summary>
        /// A <see cref="DelegatingHandler"/> that does not dispose its <see cref="DelegatingHandler.InnerHandler"/>
        /// </summary>
        private class NonDisposingHandler(HttpMessageHandler innerHandler) : DelegatingHandler(innerHandler)
        {
            protected override void Dispose(bool disposing)
            {
                // The InnerHandler is shared by all HttpClient instances created by
                // Client.CreateHttpClient, its lifetime is managed by Client.DisposeAsync
            }
        }
    }
}
