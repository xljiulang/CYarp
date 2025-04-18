using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Forwarder;

namespace CYarp.Server.Clients
{
    sealed class ClientHttpTransformer : HttpTransformer
    {
        private readonly HttpTransformer inner = Default;

        public override async ValueTask TransformRequestAsync(HttpContext httpContext, HttpRequestMessage proxyRequest, string destinationPrefix, CancellationToken cancellationToken)
        {
            await inner.TransformRequestAsync(httpContext, proxyRequest, destinationPrefix, cancellationToken);
            proxyRequest.Headers.Host = null;
        }

        public override ValueTask<bool> TransformResponseAsync(HttpContext httpContext, HttpResponseMessage? proxyResponse, CancellationToken cancellationToken)
        {
            return inner.TransformResponseAsync(httpContext, proxyResponse, cancellationToken);
        }

        public override ValueTask TransformResponseTrailersAsync(HttpContext httpContext, HttpResponseMessage proxyResponse, CancellationToken cancellationToken)
        {
            return inner.TransformResponseTrailersAsync(httpContext, proxyResponse, cancellationToken);
        }
    }
}
