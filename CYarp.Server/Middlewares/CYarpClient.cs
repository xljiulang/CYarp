using CYarp.Server.Clients;
using CYarp.Server.Configs;
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Forwarder;

namespace CYarp.Server.Middlewares
{
    [DebuggerDisplay("Id = {Id}")]
    sealed class CYarpClient : ClientBase
    {
        private readonly Stream stream;
        private readonly CancellationTokenSource disposeTokenSource = new();

        public CYarpClient(
            Stream stream,
            IHttpForwarder httpForwarder,
            HttpHandlerConfig httpHandlerConfig,
            TunnelStreamFactory tunnelStreamFactory,
            string clientId,
            Uri clientDestination,
            ClaimsPrincipal clientUser) : base(httpForwarder, httpHandlerConfig, tunnelStreamFactory, clientId, clientDestination, clientUser)
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
                var token = this.disposeTokenSource.Token;
                while (true)
                {
                    var read = await this.stream.ReadAsync(buffer, token);
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

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            this.disposeTokenSource.Cancel();
            this.disposeTokenSource.Dispose();
            this.stream.Dispose();
        }
    }
}
