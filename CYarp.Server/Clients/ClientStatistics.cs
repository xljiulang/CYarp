using System.Threading;

namespace CYarp.Server.Clients
{
    sealed class ClientStatistics
    {
        private int tcpTunnelCount = 0;
        private int httpTunnelCount = 0;

        public int TcpTunnelCount => this.tcpTunnelCount;

        public int HttpTunnelCount => this.httpTunnelCount;

        public int AddTcpTunnelCount(int value)
        {
            return Interlocked.Add(ref this.tcpTunnelCount, value);
        }

        public int AddHttpTunnelCount(int value)
        {
            return Interlocked.Add(ref this.httpTunnelCount, value);
        }
    }
}
