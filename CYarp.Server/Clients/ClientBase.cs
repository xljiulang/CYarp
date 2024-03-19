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
        private readonly Lazy<ClientHttpHandler> httpHandlerLazy;

        public string Id { get; }
        public Uri Destination { get; }

        public ClientBase(
            HttpHandlerConfig httpHandlerConfig,
            IHttpForwarder httpForwarder,
            TunnelStreamFactory tunnelStreamFactory,
            string clientId,
            Uri clientDestination)
        {
            this.httpForwarder = httpForwarder;
            this.httpHandlerLazy = new Lazy<ClientHttpHandler>(() => new ClientHttpHandler(httpHandlerConfig, tunnelStreamFactory, this));

            this.Id = clientId;
            this.Destination = clientDestination;
        }


        public abstract Task CreateTunnelAsync(Guid tunnelId, CancellationToken cancellationToken = default);


        public ValueTask<ForwarderError> ForwardHttpAsync(HttpContext context, ForwarderRequestConfig? requestConfig = default, HttpTransformer? transformer = default)
        {
            var httpHandler = httpHandlerLazy.Value;
            var httpClient = new HttpMessageInvoker(httpHandler, disposeHandler: false);
            return this.httpForwarder.SendAsync(context, this.Destination.OriginalString, httpClient, requestConfig ?? ForwarderRequestConfig.Empty, transformer ?? HttpTransformer.Empty);
        }

        public abstract Task WaitForCloseAsync();


        public virtual void Dispose()
        {
            if (this.httpHandlerLazy.IsValueCreated)
            {
                this.httpHandlerLazy.Value.Dispose();
            }
        }

        public override string ToString()
        {
            return this.Id;
        }
    }
}