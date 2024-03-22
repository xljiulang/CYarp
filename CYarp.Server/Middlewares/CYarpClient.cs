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
        private readonly SignalTunnel signalTunnel;
        private readonly SignalTunnelConfig signalTunnelConfig;
        private readonly Timer? keepAliveTimer;
        private readonly CancellationTokenSource disposeTokenSource = new();

        private static readonly int bufferSize = 8;
        private static readonly string Ping = "PING";
        private static readonly ReadOnlyMemory<byte> PingLine = "PING\r\n"u8.ToArray();
        private static readonly ReadOnlyMemory<byte> PongLine = "PONG\r\n"u8.ToArray();

        public CYarpClient(
            SignalTunnel signalTunnel,
            IHttpForwarder httpForwarder,
            SignalTunnelConfig signalTunnelConfig,
            HttpTunnelConfig httpTunnelConfig,
            HttpTunnelFactory httpTunnelFactory,
            string clientId,
            Uri clientDestination,
            ClaimsPrincipal clientUser) : base(httpForwarder, httpTunnelConfig, httpTunnelFactory, clientId, clientDestination, clientUser)
        {
            this.signalTunnel = signalTunnel;
            this.signalTunnelConfig = signalTunnelConfig;

            var interval = signalTunnelConfig.KeepAliveInterval;
            if (interval > TimeSpan.Zero)
            {
                this.keepAliveTimer = new Timer(this.KeepAliveTimerTick, null, interval, interval);
            }
        }

        /// <summary>
        /// 心跳timer
        /// </summary>
        /// <param name="state"></param>
        private async void KeepAliveTimerTick(object? state)
        {
            try
            {
                await this.signalTunnel.WriteAsync(PingLine);
            }
            catch (Exception)
            {
                this.keepAliveTimer?.Dispose();
            }
        }

        public override async Task CreateTunnelAsync(Guid tunnelId, CancellationToken cancellationToken = default)
        {
            base.ValidateDisposed();

            var tunnelText = $"{tunnelId}\r\n";
            var buffer = Encoding.UTF8.GetBytes(tunnelText);
            await this.signalTunnel.WriteAsync(buffer, cancellationToken);
        }

        public override async Task WaitForCloseAsync()
        {
            try
            {
                var cancellationToken = this.disposeTokenSource.Token;
                await this.WaitForCloseCoreAsync(cancellationToken);
            }
            catch (Exception)
            {
            }
        }

        private async Task WaitForCloseCoreAsync(CancellationToken cancellationToken)
        {
            using var reader = new StreamReader(this.signalTunnel, Encoding.UTF8, false, bufferSize, leaveOpen: true);
            while (cancellationToken.IsCancellationRequested == false)
            {
                using var timeoutTokenSource = new CancellationTokenSource(this.signalTunnelConfig.GetTimeout());
                using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutTokenSource.Token);
                var text = await reader.ReadLineAsync(linkedTokenSource.Token);

                if (text == null)
                {
                    break;
                }
                else if (text == Ping)
                {
                    await this.signalTunnel.WriteAsync(PongLine, cancellationToken);
                }
            }
        }


        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            this.disposeTokenSource.Cancel();
            this.disposeTokenSource.Dispose();
            this.signalTunnel.Dispose();
            this.keepAliveTimer?.Dispose();
        }
    }
}
