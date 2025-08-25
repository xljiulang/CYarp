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
    /// 客户端
    /// </summary>
    [DebuggerDisplay("Id = {Id}, Protocol = {Protocol}")]
    sealed class Client : IClient
    {
        private volatile bool disposed = false;
        private readonly ClientConnection connection;
        private readonly IHttpForwarder httpForwarder;
        private readonly TunnelFactory tunnelFactory;
        private readonly HttpTunnelConfig httpTunnelConfig;
        private readonly Lazy<HttpMessageInvoker> httpClientLazy;
        private readonly TunnelStatistics tunnelStatistics = new();

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
            this.httpTunnelConfig = httpTunnelConfig;

            this.TargetUri = targetUri;
            this.Protocol = protocol;
            this.User = user;

            this.httpClientLazy = new Lazy<HttpMessageInvoker>(this.CreateHttpClient);
        }

        private HttpMessageInvoker CreateHttpClient()
        {
            var httpHandler = new ClientHttpHandler(this.connection, this.tunnelFactory, this.httpTunnelConfig, this.tunnelStatistics);
            return new HttpMessageInvoker(httpHandler, disposeHandler: true);
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

            var httpClient = this.httpClientLazy.Value;
            var destination = this.TargetUri.OriginalString;
            var requestConfig = this.TargetUri.Scheme == Uri.UriSchemeHttp ? httpRequestConfig : httpsRequestConfig;
            return this.httpForwarder.SendAsync(context, destination, httpClient, requestConfig, transformer ?? HttpTransformer.Default);
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