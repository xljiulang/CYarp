using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CYarp.Server.Clients
{
    /// <summary>
    /// 客户端状态后台服务
    /// </summary>
    sealed class ClientStateStorageService : BackgroundService
    {
        private readonly ClientStateChannel clientStateChannel;
        private readonly IEnumerable<IClientStateStorage> stateStorages;

        public ClientStateStorageService(
            ClientStateChannel clientStateChannel,
            IEnumerable<IClientStateStorage> stateStorages)
        {
            this.clientStateChannel = clientStateChannel;
            this.stateStorages = stateStorages;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (this.stateStorages.Any())
            {
                await this.ResetClientStatesAsync(stoppingToken);
                await this.ConsumeClientStatesAsync(stoppingToken);
            }
        }

        /// <summary>
        /// 重置节点的ClientState
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task ResetClientStatesAsync(CancellationToken cancellationToken)
        {
            foreach (var storage in this.stateStorages)
            {
                await storage.ResetClientStatesAsync( cancellationToken);
            }
        }

        /// <summary>
        /// 从channel消费ClientState
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task ConsumeClientStatesAsync(CancellationToken cancellationToken)
        {
            await foreach (var clientState in this.clientStateChannel.ReadAllAsync(cancellationToken))
            {
                foreach (var storage in this.stateStorages)
                {
                    await storage.WriteClientStateAsync(clientState, cancellationToken);
                }
            }
        }
    }
}
