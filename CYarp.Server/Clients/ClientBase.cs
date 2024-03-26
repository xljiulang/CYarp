using CYarp.Server.Configs;
using Microsoft.AspNetCore.Http;
using System;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Forwarder;

namespace CYarp.Server.Clients
{
    /// <summary>
    /// 客户端抽象类
    /// </summary>
    abstract class ClientBase : IClient, IConnection
    {
        private readonly IHttpForwarder httpForwarder;
        private readonly HttpContext httpContext;
        private readonly Lazy<HttpMessageInvoker> httpClientLazy;
        private readonly CancellationTokenSource disposeTokenSource = new();
        private static readonly ForwarderRequestConfig forwarderRequestConfig = new() { Version = HttpVersion.Version11, VersionPolicy = HttpVersionPolicy.RequestVersionExact };

        public string Id { get; }

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


        public ClientBase(
            IHttpForwarder httpForwarder,
            HttpTunnelConfig httpTunnelConfig,
            HttpTunnelFactory httpTunnelFactory,
            string clientId,
            Uri clientTargetUri,
            HttpContext httpContext)
        {
            this.httpForwarder = httpForwarder;
            this.httpClientLazy = new Lazy<HttpMessageInvoker>(() =>
            {
                var httpHandler = new ClientHttpHandler(httpTunnelConfig, httpTunnelFactory, this);
                return new HttpMessageInvoker(httpHandler);
            });

            this.Id = clientId;
            this.TargetUri = clientTargetUri;
            this.httpContext = httpContext;
        }


        public ValueTask<ForwarderError> ForwardHttpAsync(HttpContext httpContext, ForwarderRequestConfig? requestConfig, HttpTransformer? transformer)
        {
            ObjectDisposedException.ThrowIf(this.disposeTokenSource.IsCancellationRequested, this);

            var httpClient = this.httpClientLazy.Value;
            var destination = this.TargetUri.OriginalString;
            return this.httpForwarder.SendAsync(httpContext, destination, httpClient, requestConfig ?? forwarderRequestConfig, transformer ?? HttpTransformer.Empty);
        }

        public abstract Task CreateHttpTunnelAsync(Guid tunnelId, CancellationToken cancellationToken);

        public async Task WaitForCloseAsync()
        {
            try
            {
                var cancellationToken = this.disposeTokenSource.Token;
                await this.HandleConnectionAsync(cancellationToken);
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// 处理连接
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected abstract Task HandleConnectionAsync(CancellationToken cancellationToken);


        public void Dispose()
        {
            if (this.disposeTokenSource.IsCancellationRequested == false)
            {
                this.disposeTokenSource.Cancel();
                this.Dispose(disposing: true);
            }
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            this.disposeTokenSource.Dispose();
            if (this.httpClientLazy.IsValueCreated)
            {
                this.httpClientLazy.Value.Dispose();
            }
        }

        public override string ToString()
        {
            return this.Id;
        }
    }
}