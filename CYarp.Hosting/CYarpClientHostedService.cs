using CYary.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;

namespace CYarp.Hosting
{
    sealed class CYarpClientHostedService : BackgroundService
    {
        private readonly IOptionsMonitor<CYarpClientOptions> clientOptions;

        public CYarpClientHostedService(IOptionsMonitor<CYarpClientOptions> clientOptions)
        {
            this.clientOptions = clientOptions;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var client = new CYarpClient();
            while (stoppingToken.IsCancellationRequested == false)
            {
                await client.TransportAsync(this.clientOptions.CurrentValue, stoppingToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            }
        }
    }
}
