using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Net;

namespace CYarp.Client.AspNetCore
{
    sealed class CYarpConnectionContext : ConnectionContext
        , IConnectionIdFeature
        , IConnectionItemsFeature
        , IConnectionEndPointFeature
        , IConnectionTransportFeature
    {
        private readonly FeatureCollection features = new();

        public CYarpConnectionContext(Stream stream, EndPoint remoteEndPoint)
        {
            this.ConnectionId = stream.ToString() ?? string.Empty;
            this.Transport = new CYarpDuplexPipe(stream);
            this.RemoteEndPoint = remoteEndPoint;

            this.features.Set<IConnectionIdFeature>(this);
            this.features.Set<IConnectionItemsFeature>(this);
            this.features.Set<IConnectionEndPointFeature>(this);
            this.features.Set<IConnectionTransportFeature>(this);
        }

        public override string ConnectionId { get; set; }

        public override IDuplexPipe Transport { get; set; }

        public override IFeatureCollection Features => this.features;

        public override IDictionary<object, object?> Items { get; set; } = new Dictionary<object, object?>();
    }
}
