using Microsoft.AspNetCore.Mvc;

namespace LibraryApp.Controllers
{
    public class UserController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
