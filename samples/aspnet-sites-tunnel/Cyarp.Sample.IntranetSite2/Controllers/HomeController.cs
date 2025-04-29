using Microsoft.AspNetCore.Mvc;

namespace Cyarp.Sample.IntranetSite2.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet]
        [Route("/")]
        public string Index()
        {
            return $"Hello from site2 {DateTime.Now.ToLongTimeString()}";
        }
    }
}
