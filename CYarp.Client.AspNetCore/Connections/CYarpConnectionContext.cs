using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace CYarp.Client.AspNetCore.Connections
{
    sealed class CYarpConnectionContext : ConnectionContext,
        IConnectionIdFeature,
        IConnectionItemsFeature,
        IConnectionEndPointFeature,
        IConnectionTransportFeature
    {
        private readonly CyarpConnection stream;
        private readonly FeatureCollection features = new();

        public override string ConnectionId { get; set; }

        public override IDuplexPipe Transport { get; set; }

        public override IFeatureCollection Features => features;

        public override IDictionary<object, object?> Items { get; set; } = new Dictionary<object, object?>();



        public CYarpConnectionContext(CyarpConnection stream)
        {
            this.stream = stream;
            Transport = new CYarpDuplexPipe(stream);
            ConnectionId = stream.ToString() ?? string.Empty;
            ConnectionClosed = stream.ConnectionClosed;

            features.Set<IConnectionIdFeature>(this);
            features.Set<IConnectionItemsFeature>(this);
            features.Set<IConnectionEndPointFeature>(this);
            features.Set<IConnectionTransportFeature>(this);
            features.Set<IConnectionLifetimeFeature>(this.stream);
        }

        public override void Abort()
        {
            this.stream.Abort();
        }

        public override ValueTask DisposeAsync()
        {
            return stream.DisposeAsync();
        }
    }
}
