using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CYarp.Client
{
    sealed class CYarpListener : ICYarpListener
    {
        private readonly CYarpConnectionFactory connectionFactory;
        private readonly CYarpConnection connection;

        public CYarpListener(CYarpConnectionFactory connectionFactory, CYarpConnection connection)
        {
            this.connectionFactory = connectionFactory;
            this.connection = connection;
        }

        public async Task<Stream?> AcceptAsync(CancellationToken cancellationToken)
        {
            var tunnelId = await this.connection.ReadTunnelIdAsync(cancellationToken);
            if (tunnelId == null)
            {
                return null;
            }

            return await this.connectionFactory.CreateServerTunnelAsync(tunnelId.Value, cancellationToken);
        }

        public ValueTask DisposeAsync()
        {
            return this.connection.DisposeAsync();
        }
    }
}
