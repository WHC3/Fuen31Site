using Microsoft.AspNetCore.Mvc;

namespace Fuen31Site.Controllers
{
    public class AbcController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
