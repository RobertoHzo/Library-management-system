using Microsoft.AspNetCore.Mvc;

namespace Library7.Controllers
{
    public class UserController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
