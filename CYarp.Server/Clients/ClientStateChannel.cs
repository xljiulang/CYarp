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
    /// 客户端状态Channel
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
        /// 将客户端状态写入Channel
        /// 确保持久层的性能不影响到ClientManager
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
        /// 从Channel读取所有客户端状态
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
            public string Id { get; } = client.Id;

            public Uri TargetUri { get; } = client.TargetUri;

            public ClaimsPrincipal User { get; } = client.User;

            public TransportProtocol Protocol { get; } = client.Protocol;

            public IPEndPoint? RemoteEndpoint { get; } = client.RemoteEndpoint;

            public int TcpTunnelCount { get; } = client.TcpTunnelCount;

            public int HttpTunnelCount { get; } = client.HttpTunnelCount;

            public DateTimeOffset CreationTime { get; } = client.CreationTime;


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
        }
    }
}
