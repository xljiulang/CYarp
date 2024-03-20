using CYarp.Server.Configs;
using Microsoft.AspNetCore.Http;
using System;
using System.Net.Http;
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
        private readonly IHttpForwarder httpForwarder;
        private readonly Lazy<HttpMessageInvoker> httpClientLazy;

        public string Id { get; }

        public Uri Destination { get; }

        public ClientBase(
            IHttpForwarder httpForwarder,
            HttpHandlerConfig httpHandlerConfig,
            TunnelStreamFactory tunnelStreamFactory,
            string clientId,
            Uri clientDestination)
        {
            this.httpForwarder = httpForwarder;
            this.httpClientLazy = new Lazy<HttpMessageInvoker>(() =>
            {
                var httpHandler = new ClientHttpHandler(httpHandlerConfig, tunnelStreamFactory, this);
                return new HttpMessageInvoker(httpHandler);
            });

            this.Id = clientId;
            this.Destination = clientDestination;
        }


        public abstract Task CreateTunnelAsync(Guid tunnelId, CancellationToken cancellationToken = default);


        public ValueTask<ForwarderError> ForwardHttpAsync(HttpContext context, ForwarderRequestConfig? requestConfig = default, HttpTransformer? transformer = default)
        {
            var httpClient = this.httpClientLazy.Value;
            var destination = this.Destination.OriginalString;
            return this.httpForwarder.SendAsync(context, destination, httpClient, requestConfig ?? ForwarderRequestConfig.Empty, transformer ?? HttpTransformer.Empty);
        }

        public abstract Task WaitForCloseAsync();


        public virtual void Dispose()
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