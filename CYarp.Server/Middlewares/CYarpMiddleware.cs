using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.Extensions.Primitives;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CYarp.Server.Middlewares
{
    sealed class CYarpMiddleware : IMiddleware
    {
        public Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var upgradeHeader = context.Request.Headers.Upgrade;
            var upgradeFeature = context.Features.GetRequiredFeature<IHttpUpgradeFeature>();
            var connectFeature = context.Features.Get<IHttpExtendedConnectFeature>();
            var requestTimeoutFeature = context.Features.Get<IHttpRequestTimeoutFeature>();

            var feature = new CYarpFeature(upgradeHeader, upgradeFeature, connectFeature, requestTimeoutFeature);
            context.Features.Set<ICYarpFeature>(feature);
            return next(context);
        }

        private class CYarpFeature : ICYarpFeature
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

            public async Task<Stream> AcceptAsync()
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
}