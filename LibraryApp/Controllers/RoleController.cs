using Microsoft.AspNetCore.Mvc;

namespace LibraryApp.Controllers
{
    public class RoleController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
