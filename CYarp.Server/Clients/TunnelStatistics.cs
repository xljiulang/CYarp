using System.Threading;

namespace CYarp.Server.Clients
{
    sealed class TunnelStatistics
    {
        private int tcpTunnelCount = 0;
        private int httpTunnelCount = 0;

        public int TcpTunnelCount => this.tcpTunnelCount;

        public int HttpTunnelCount => this.httpTunnelCount;

        public int AddTunnelCount(TunnelType tunnelType, int value)
        {
            return tunnelType == TunnelType.HttpTunnel
                ? Interlocked.Add(ref this.httpTunnelCount, value)
                : Interlocked.Add(ref this.tcpTunnelCount, value);
        }
    }
}
