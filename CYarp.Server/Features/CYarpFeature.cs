using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.Extensions.Primitives;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CYarp.Server.Features
{
    sealed class CYarpFeature : ICYarpFeature
    {
        private const string CYarp = "CYarp";
        private readonly IHttpUpgradeFeature upgradeFeature;
        private readonly IHttpExtendedConnectFeature? connectFeature;
        private readonly IHttpRequestTimeoutFeature? requestTimeoutFeature;

        private readonly bool cyarpRequest;

        public bool IsCYarpRequest => this.cyarpRequest;

        public CYarpFeature(
            StringValues upgradeHeader,
            IHttpUpgradeFeature upgradeFeature,
            IHttpExtendedConnectFeature? connectFeature,
            IHttpRequestTimeoutFeature? requestTimeoutFeature)
        {
            this.upgradeFeature = upgradeFeature;
            this.connectFeature = connectFeature;
            this.requestTimeoutFeature = requestTimeoutFeature;

            this.cyarpRequest = (upgradeFeature.IsUpgradableRequest && string.Equals(CYarp, upgradeHeader, StringComparison.InvariantCultureIgnoreCase)) ||
                (connectFeature != null && connectFeature.IsExtendedConnect && string.Equals(CYarp, connectFeature.Protocol, StringComparison.InvariantCultureIgnoreCase));
        }


        public async Task<Stream> AcceptAsStreamAsync()
        {
            if (this.cyarpRequest == false)
            {
                throw new InvalidOperationException("Not a CYarp request");
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

            throw new InvalidOperationException("Not a CYarp request");
        }

        public async Task<Stream> AcceptAsSafeWriteStreamAsync()
        {
            if (this.cyarpRequest == false)
            {
                throw new InvalidOperationException("Not a CYarp request");
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

            throw new InvalidOperationException("Not a CYarp request");
        }
    }
}
