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
            private static readonly StringValues cyarp = new("CYarp");
            private readonly StringValues upgradeHeader;
            private readonly IHttpUpgradeFeature upgradeFeature;
            private readonly IHttpExtendedConnectFeature? connectFeature;
            private readonly IHttpRequestTimeoutFeature? requestTimeoutFeature;

            public bool IsCYarpRequest
            {
                get
                {
                    return (this.upgradeFeature.IsUpgradableRequest && this.upgradeHeader == cyarp) ||
                        (this.connectFeature != null && connectFeature.IsExtendedConnect && connectFeature.Protocol == cyarp);
                }
            }


            public CYarpFeature(
                StringValues upgradeHeader,
                IHttpUpgradeFeature upgradeFeature,
                IHttpExtendedConnectFeature? connectFeature,
                IHttpRequestTimeoutFeature? requestTimeoutFeature)
            {
                this.upgradeHeader = upgradeHeader;
                this.upgradeFeature = upgradeFeature;
                this.connectFeature = connectFeature;
                this.requestTimeoutFeature = requestTimeoutFeature;
            }

            public async Task<Stream> AcceptAsync()
            {
                if (this.upgradeFeature.IsUpgradableRequest && this.upgradeHeader == cyarp)
                {
                    this.requestTimeoutFeature?.DisableTimeout();
                    return await this.upgradeFeature.UpgradeAsync();
                }

                if (this.connectFeature != null && this.connectFeature.IsExtendedConnect && this.connectFeature.Protocol == cyarp)
                {
                    this.requestTimeoutFeature?.DisableTimeout();
                    return await this.connectFeature.AcceptAsync();
                }

                throw new InvalidOperationException("Not a CYarp request");
            }
        }
    }
}