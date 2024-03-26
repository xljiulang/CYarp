using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace CYarp.Server.Clients
{
    /// <summary>
    /// 客户端状态Channel
    /// </summary>
    sealed class ClientStateChannel
    {
        private readonly bool hasStateStorages;
        private readonly IOptionsMonitor<CYarpOptions> cyarpOptions;
        private readonly Channel<ClientState> channel = Channel.CreateUnbounded<ClientState>();

        public ClientStateChannel(
            IEnumerable<IClientStateStorage> stateStorages,
            IOptionsMonitor<CYarpOptions> cyarpOptions)
        {
            this.hasStateStorages = stateStorages.Any();
            this.cyarpOptions = cyarpOptions;
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
                Node = this.cyarpOptions.CurrentValue.Node,
                Client = client,
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
    }
}
