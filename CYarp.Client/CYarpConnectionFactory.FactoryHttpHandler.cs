using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CYarp.Client
{
    sealed partial class CYarpConnectionFactory
    {
        private class FactoryHttpHandler : DelegatingHandler
        {
            private readonly CYarpConnectionFactory factory;

            public FactoryHttpHandler(CYarpConnectionFactory factory, HttpMessageHandler innerHandler)
            {
                this.factory = factory;
                this.InnerHandler = innerHandler;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var httpResponse = await base.SendAsync(request, cancellationToken);
                if (factory.serverHttp2Supported == null)
                {
                    factory.serverHttp2Supported = httpResponse.Version == HttpVersion.Version20;
                }

                if (httpResponse.StatusCode == HttpStatusCode.Unauthorized)
                {
                    var inner = new HttpRequestException(httpResponse.ReasonPhrase, null, httpResponse.StatusCode);
                    throw new CYarpConnectException(CYarpConnectError.Unauthorized, inner);
                }
                return httpResponse;
            }
        }
    }
}
