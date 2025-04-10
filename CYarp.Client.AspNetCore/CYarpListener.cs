using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace CYarp.Client.AspNetCore
{
    sealed class CYarpListener : IConnectionListener
    {
        private CYarpClient? client;
        private ICYarpListener? listener;

        private readonly ILogger logger;
        private readonly CYarpClientOptions options;
        public EndPoint EndPoint { get; }

        public CYarpListener(CYarpEndPoint endPoint, ILogger logger)
        {
            this.EndPoint = endPoint;
            this.logger = logger;
            this.options = endPoint.Options;
        }

        public async ValueTask<ConnectionContext?> AcceptAsync(CancellationToken cancellationToken = default)
        {
            if (this.client == null)
            {
                this.client = new CYarpClient(this.options, this.logger);
            }

            while (true)
            {
                if (this.listener == null)
                {
                    this.listener = await this.client.ListenAsync(cancellationToken);
                }

                var stream = await this.listener.AcceptAsync(cancellationToken);
                if (stream == null)
                {
                    await this.listener.DisposeAsync();
                    this.listener = null;
                }
                else
                {
                    return new CYarpConnectionContext(stream, this.EndPoint);
                }
            }
        }


        public async ValueTask DisposeAsync()
        {
            if (this.listener != null)
            {
                await this.listener.DisposeAsync();
                this.listener = null;
            }

            if (this.client != null)
            {
                this.client.Dispose();
                this.client = null;
            }
        }

        public ValueTask UnbindAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }
}
