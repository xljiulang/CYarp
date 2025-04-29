using CYarp.Server;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Cyarp.Sample.IntranetSite2.Controllers
{
    public class DomainProxyController : Controller
    {
        private readonly IClientViewer clientViewer;
        private readonly ILogger<DomainProxyController> logger;

        public DomainProxyController(IClientViewer clientViewer, ILogger<DomainProxyController> logger)
        {
            this.clientViewer = clientViewer;
            this.logger = logger;
        }
        [HttpGet]
        [Route("/")]
        public async Task Index()
        {
            var domain = HttpContext.Request.Host.Host.ToLower();
            if (clientViewer.TryGetValue(domain, out var client))
            {
                logger.LogInformation($"Forwarding request to {domain}");
                await client.ForwardHttpAsync(HttpContext);
            }
            else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status502BadGateway;
            }
        }
    }
}
