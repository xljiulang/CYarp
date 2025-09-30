using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace CYarp.Client.AspNetCore.Connections
{
    sealed class CYarpConnectionContext : ConnectionContext,
        IConnectionIdFeature,
        IConnectionItemsFeature,
        IConnectionEndPointFeature,
        IConnectionTransportFeature
    {
        private readonly Stream stream;
        private readonly FeatureCollection features = new();
        private readonly CancellationTokenSource connectionClosedTokenSource = new();

        public override string ConnectionId { get; set; }

        public override IDuplexPipe Transport { get; set; }

        public override IFeatureCollection Features => features;

        public override IDictionary<object, object?> Items { get; set; } = new Dictionary<object, object?>();

        public override CancellationToken ConnectionClosed => connectionClosedTokenSource.Token;

        public CYarpConnectionContext(Stream stream)
        {
            this.stream = stream;
            Transport = new CYarpDuplexPipe(stream);
            ConnectionId = stream.ToString() ?? string.Empty;

            features.Set<IConnectionIdFeature>(this);
            features.Set<IConnectionItemsFeature>(this);
            features.Set<IConnectionEndPointFeature>(this);
            features.Set<IConnectionTransportFeature>(this);
        }

        /// <summary>
        /// Abort the connection to trigger ConnectionClosed cancellation token
        /// </summary>
        public override void Abort()
        {
            connectionClosedTokenSource.Cancel();
        }

        public override ValueTask DisposeAsync()
        {
            connectionClosedTokenSource.Cancel();
            connectionClosedTokenSource.Dispose();
            return stream.DisposeAsync();
        }
    }
}
