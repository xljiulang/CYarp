using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace CYarp.Client.AspNetCore.Connections
{
    sealed class CYarpConnectionListenerFactory : IConnectionListenerFactory, IConnectionListenerFactorySelector
    {
        private readonly ILoggerFactory loggerFactory;
        private readonly SocketTransportFactory socketTransportFactory;

        public CYarpConnectionListenerFactory(IOptions<SocketTransportOptions> options, ILoggerFactory loggerFactory)
        {
            this.loggerFactory = loggerFactory;
            socketTransportFactory = new SocketTransportFactory(options, loggerFactory);
        }

        public bool CanBind(EndPoint endpoint)
        {
            return socketTransportFactory.CanBind(endpoint) || endpoint is CYarpEndPoint;
        }

        public async ValueTask<IConnectionListener> BindAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
        {
            if (endpoint is CYarpEndPoint cyarpEndPoint)
            {
                var logger = loggerFactory.CreateLogger<CYarpConnectionListener>();
                return new CYarpConnectionListener(cyarpEndPoint, logger);
            }

            return await socketTransportFactory.BindAsync(endpoint, cancellationToken);
        }
    }
}
