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
    abstract class ClientBase : IClient
    {
        private volatile bool disposed = false;
        private readonly IHttpForwarder httpForwarder;
        private readonly Lazy<HttpMessageInvoker> httpClientLazy;
        private static readonly ForwarderRequestConfig forwarderRequestConfig = new() { Version = HttpVersion.Version11, VersionPolicy = HttpVersionPolicy.RequestVersionExact };

        public string Id { get; }

        public Uri Destination { get; }

        public ClaimsPrincipal User { get; }

        public ClientBase(
            IHttpForwarder httpForwarder,
            HttpTunnelConfig httpTunnelConfig,
            HttpTunnelFactory httpTunnelFactory,
            string clientId,
            Uri clientDestination,
            ClaimsPrincipal clientUser)
        {
            this.httpForwarder = httpForwarder;
            this.httpClientLazy = new Lazy<HttpMessageInvoker>(() =>
            {
                var httpHandler = new ClientHttpHandler(httpTunnelConfig, httpTunnelFactory, this);
                return new HttpMessageInvoker(httpHandler);
            });

            this.Id = clientId;
            this.Destination = clientDestination;
            this.User = clientUser;
        }


        public ValueTask<ForwarderError> ForwardHttpAsync(HttpContext httpContext, ForwarderRequestConfig? requestConfig, HttpTransformer? transformer)
        {
            this.ValidateDisposed();

            var httpClient = this.httpClientLazy.Value;
            var destination = this.Destination.OriginalString;
            return this.httpForwarder.SendAsync(httpContext, destination, httpClient, requestConfig ?? forwarderRequestConfig, transformer ?? HttpTransformer.Empty);
        }

        public abstract Task CreateTunnelAsync(Guid tunnelId, CancellationToken cancellationToken);

        public abstract Task WaitForCloseAsync();

        protected void ValidateDisposed()
        {
            ObjectDisposedException.ThrowIf(this.disposed, this);
        }

        public void Dispose()
        {
            if (this.disposed == false)
            {
                this.disposed = true;
                this.Dispose(disposing: true);
            }
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
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