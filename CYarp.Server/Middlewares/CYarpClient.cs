using CYarp.Server.Clients;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Forwarder;

namespace CYarp.Server.Middlewares
{
    sealed class CYarpClient : ClientBase
    {
        private readonly Stream stream;

        public CYarpClient(
            Stream stream,
            IHttpForwarder httpForwarder,
            TunnelStreamFactory tunnelStreamFactory,
            string clientId,
            Uri clientDestination) : base(httpForwarder, tunnelStreamFactory, clientId, clientDestination)
        {
            this.stream = stream;
        }

        public override async Task CreateTunnelAsync(Guid tunnelId, CancellationToken cancellationToken = default)
        {
            var text = $"{tunnelId}\r\n";
            var buffer = Encoding.UTF8.GetBytes(text);
            await this.stream.WriteAsync(buffer, cancellationToken);
            await this.stream.FlushAsync(cancellationToken);
        }

        public override async Task WaitForCloseAsync()
        {
            try
            {
                var buffer = new byte[8].AsMemory();
                while (true)
                {
                    var read = await this.stream.ReadAsync(buffer);
                    if (read == 0)
                    {
                        break;
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        public override void Dispose()
        {
            this.stream.Dispose();
            base.Dispose();
        }
    }
}
