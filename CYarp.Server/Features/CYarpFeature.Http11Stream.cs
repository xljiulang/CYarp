using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace CYarp.Server.Features
{
    sealed partial class CYarpFeature
    {
        private class Http11Stream : DelegatingStream
        {
            private readonly Socket? connection;

            public Http11Stream(Stream inner, Socket? connection)
                : base(inner)
            {
                this.connection = connection;
            }

            private void CloseConnection()
            {
                if (connection != null && connection.Connected)
                {
                    var stream = new NetworkStream(connection, ownsSocket: true);
                    stream.Dispose();
                }
            }

            public override async ValueTask DisposeAsync()
            {
                await this.Inner.DisposeAsync();
                this.CloseConnection();
            }

            protected override void Dispose(bool disposing)
            {
                this.Inner.Dispose();
                this.CloseConnection();
            }
        }
    }
}
