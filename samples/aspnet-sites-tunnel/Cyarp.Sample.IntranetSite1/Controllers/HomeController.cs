using Microsoft.AspNetCore.Mvc;

namespace Cyarp.Sample.IntranetSite1.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet]
        [Route("/")]
        public string Index()
        {
            return $"Hello from site1 {DateTime.Now.ToLongTimeString()}";
        }
    }
}
