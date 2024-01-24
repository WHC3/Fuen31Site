using Microsoft.AspNetCore.Mvc;

namespace Fuen31Site.Controllers
{
    public class HomeworkController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }


        public IActionResult CheckAccount()
        {
            return View();
        }
    }
}
