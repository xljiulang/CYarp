﻿using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace CYarp.Client.AspNetCore
{
    sealed class CYarpListenerFactory : IConnectionListenerFactory, IConnectionListenerFactorySelector
    {
        private readonly ILoggerFactory loggerFactory;
        private readonly SocketTransportFactory socketTransportFactory;

        public CYarpListenerFactory(IOptions<SocketTransportOptions> options, ILoggerFactory loggerFactory)
        {
            this.loggerFactory = loggerFactory;
            this.socketTransportFactory = new SocketTransportFactory(options, loggerFactory);
        }

        public bool CanBind(EndPoint endpoint)
        {
            return this.socketTransportFactory.CanBind(endpoint) || endpoint is CYarpEndPoint;
        }

        public async ValueTask<IConnectionListener> BindAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
        {
            if (endpoint is CYarpEndPoint cyarpEndPoint)
            {
                var logger = this.loggerFactory.CreateLogger<CYarpListener>();
                return new CYarpListener(cyarpEndPoint, logger);
            }

            return await this.socketTransportFactory.BindAsync(endpoint, cancellationToken);
        }
    }
}
