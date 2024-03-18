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
        private readonly Lazy<HttpTunnelHandler> httpTunnelHandlerLazy;

        public string Id { get; }
        public Uri Destination { get; }

        public ClientBase(
            IHttpForwarder httpForwarder,
            TunnelStreamFactory tunnelStreamFactory,
            string clientId,
            Uri clientDestination)
        {
            this.httpForwarder = httpForwarder;
            httpTunnelHandlerLazy = new Lazy<HttpTunnelHandler>(() => new HttpTunnelHandler(tunnelStreamFactory, this));

            Id = clientId;
            Destination = clientDestination;
        }

        /// <summary>
        /// 请求创建tunnel
        /// </summary>
        /// <param name="tunnelId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public abstract Task CreateTunnelAsync(Guid tunnelId, CancellationToken cancellationToken = default);


        public ValueTask<ForwarderError> ForwardHttpAsync(HttpContext context)
        {
            var httpHandler = httpTunnelHandlerLazy.Value;
            var httpClient = new HttpMessageInvoker(httpHandler, disposeHandler: false);
            return httpForwarder.SendAsync(context, Destination.OriginalString, httpClient, ForwarderRequestConfig.Empty, HttpTransformer.Empty);
        }

        public abstract Task WaitForCloseAsync();


        public virtual void Dispose()
        {
            if (httpTunnelHandlerLazy.IsValueCreated)
            {
                httpTunnelHandlerLazy.Value.Dispose();
            }
        }

        public override string ToString()
        {
            return Id;
        }
    }
}