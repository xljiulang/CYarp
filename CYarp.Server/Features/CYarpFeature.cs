using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.Extensions.Primitives;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CYarp.Server.Features
{
    sealed class CYarpFeature : ICYarpFeature
    {
        private const string CYarp = "CYarp";
        private readonly WebSocketManager webSocketManager;
        private readonly IHttpUpgradeFeature upgradeFeature;
        private readonly IHttpExtendedConnectFeature? connectFeature;
        private readonly IHttpRequestTimeoutFeature? requestTimeoutFeature;

        public bool IsCYarpRequest { get; }

        public TransportProtocol Protocol { get; }

        public CYarpFeature(
            bool isHttp2,
            WebSocketManager webSocketManager,
            StringValues upgradeHeader,
            IHttpUpgradeFeature upgradeFeature,
            IHttpExtendedConnectFeature? connectFeature,
            IHttpRequestTimeoutFeature? requestTimeoutFeature)
        {
            this.webSocketManager = webSocketManager;
            this.upgradeFeature = upgradeFeature;
            this.connectFeature = connectFeature;
            this.requestTimeoutFeature = requestTimeoutFeature;

            if (this.webSocketManager.IsWebSocketRequest &&
                this.webSocketManager.WebSocketRequestedProtocols.Contains(CYarp, StringComparer.InvariantCultureIgnoreCase))
            {
                this.IsCYarpRequest = true;
                this.Protocol = isHttp2 ? TransportProtocol.WebSocketWithHttp2 : TransportProtocol.WebSocketWithHttp11;
            }

            if ((upgradeFeature.IsUpgradableRequest && string.Equals(CYarp, upgradeHeader, StringComparison.InvariantCultureIgnoreCase)) ||
                (connectFeature != null && connectFeature.IsExtendedConnect && string.Equals(CYarp, connectFeature.Protocol, StringComparison.InvariantCultureIgnoreCase)))
            {
                this.IsCYarpRequest = true;
                this.Protocol = isHttp2 ? TransportProtocol.HTTP2 : TransportProtocol.Http11;
            }
        }

        public async Task<Stream> AcceptAsStreamAsync()
        {
            if (this.IsCYarpRequest == false)
            {
                throw new InvalidOperationException("Not a CYarp request");
            }

            if (this.webSocketManager.IsWebSocketRequest)
            {
                var webSocket = await this.webSocketManager.AcceptWebSocketAsync();
                return new WebSocketStream(webSocket);
            }

            if (this.upgradeFeature.IsUpgradableRequest)
            {
                this.requestTimeoutFeature?.DisableTimeout();
                return await this.upgradeFeature.UpgradeAsync();
            }

            if (this.connectFeature != null)
            {
                this.requestTimeoutFeature?.DisableTimeout();
                return await this.connectFeature.AcceptAsync();
            }

            throw new NotImplementedException();
        }

        public async Task<Stream> AcceptAsSafeWriteStreamAsync()
        {
            var stream = await this.AcceptAsStreamAsync();
            return new SafeWriteStream(stream);
        }
    }
}
