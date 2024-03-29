using CYarp.Server.Configs;
using Microsoft.AspNetCore.Http;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
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
        private readonly HttpTunnelConfig httpTunnelConfig;
        private readonly HttpTunnelFactory httpTunnelFactory;
        private readonly HttpContext httpContext;
        private readonly Lazy<HttpMessageInvoker> httpClientLazy;
        private static readonly ForwarderRequestConfig forwarderRequestConfig = new() { Version = HttpVersion.Version11, VersionPolicy = HttpVersionPolicy.RequestVersionExact };

        public string Id => this.connection.ClientId;

        public Uri TargetUri { get; }

        public ClaimsPrincipal User => this.httpContext.User;

        public string Protocol => this.httpContext.Request.Protocol;

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
            HttpTunnelConfig httpTunnelConfig,
            HttpTunnelFactory httpTunnelFactory,
            Uri clientTargetUri,
            HttpContext httpContext)
        {
            this.connection = connection;
            this.httpForwarder = httpForwarder;
            this.httpTunnelConfig = httpTunnelConfig;
            this.httpTunnelFactory = httpTunnelFactory;
            this.TargetUri = clientTargetUri;
            this.httpContext = httpContext;

            this.httpClientLazy = new Lazy<HttpMessageInvoker>(this.CreateHttpClient);
        }

        private HttpMessageInvoker CreateHttpClient()
        {
            var httpHandler = new ClientHttpHandler(this.httpTunnelConfig, this.httpTunnelFactory, this.connection);
            return new HttpMessageInvoker(httpHandler);
        }


        public ValueTask<ForwarderError> ForwardHttpAsync(HttpContext httpContext, ForwarderRequestConfig? requestConfig, HttpTransformer? transformer)
        {
            ObjectDisposedException.ThrowIf(this.disposed, this);

            var httpClient = this.httpClientLazy.Value;
            var destination = this.TargetUri.OriginalString;
            return this.httpForwarder.SendAsync(httpContext, destination, httpClient, requestConfig ?? forwarderRequestConfig, transformer ?? HttpTransformer.Empty);
        }


        public void Dispose()
        {
            if (this.disposed == false)
            {
                this.disposed = true;
                this.DisposeCore();
            }
            GC.SuppressFinalize(this);
        }

        private void DisposeCore()
        {
            if (this.httpClientLazy.IsValueCreated)
            {
                this.httpClientLazy.Value.Dispose();
            }
            this.connection.Dispose();
        }

        public override string ToString()
        {
            return this.Id;
        }
    }
}