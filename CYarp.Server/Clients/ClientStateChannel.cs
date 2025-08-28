using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Forwarder;

namespace CYarp.Server.Clients
{
    /// <summary>
    /// ClientStateChannel
    /// </summary>
    sealed class ClientStateChannel
    {
        private readonly bool hasStateStorages;
        private readonly Channel<ClientState> channel = Channel.CreateUnbounded<ClientState>();

        public ClientStateChannel(IEnumerable<IClientStateStorage> stateStorages)
        {
            this.hasStateStorages = stateStorages.Any();
        }

        /// <summary>
        /// WillClientState写入Channel
        /// 确保持久层性能不影响ToClientManager
        /// </summary>
        /// <param name="client"></param>
        /// <param name="connected"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask WriteAsync(IClient client, bool connected, CancellationToken cancellationToken)
        {
            if (this.hasStateStorages == false)
            {
                return ValueTask.CompletedTask;
            }

            var clientState = new ClientState
            {
                Client = new ReadOnlyClient(client),
                IsConnected = connected
            };

            return this.channel.Writer.WriteAsync(clientState, cancellationToken);
        }

        /// <summary>
        /// 从Channel读取所有ClientState
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public IAsyncEnumerable<ClientState> ReadAllAsync(CancellationToken cancellationToken)
        {
            return this.channel.Reader.ReadAllAsync(cancellationToken);
        }


        [DebuggerDisplay("Id = {Id}, Protocol = {Protocol}")]
        private sealed class ReadOnlyClient(IClient client) : IClient
        {
            public string Id => client.Id;

            public Uri TargetUri => client.TargetUri;

            public ClaimsPrincipal User => client.User;

            public TransportProtocol Protocol => client.Protocol;

            public IPEndPoint? RemoteEndpoint => client.RemoteEndpoint;

            public int TcpTunnelCount => client.TcpTunnelCount;

            public int HttpTunnelCount => client.HttpTunnelCount;

            public DateTimeOffset CreationTime => client.CreationTime;


            public Task<Stream> CreateTcpTunnelAsync(CancellationToken cancellationToken = default)
            {
                throw new InvalidOperationException();
            }

            public ValueTask<ForwarderError> ForwardHttpAsync(HttpContext context, HttpTransformer? transformer = null)
            {
                throw new InvalidOperationException();
            }

            public ValueTask DisposeAsync()
            {
                throw new InvalidOperationException();
            }

            public override string ToString()
            {
                return this.Id;
            }
        }
    }
}
