using Library7.Models;
using LibraryApp.Data;
using LibraryApp.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;

namespace Library7.Controllers
{
	[Authorize]
	public class HomeController : Controller
	{
		private readonly ILogger<HomeController> _logger;
        private readonly Library7Context _context;
        private readonly IHubContext<SignalRHub> _hubContext;

        public HomeController(ILogger<HomeController> logger, Library7Context context, IHubContext<SignalRHub> hubContext)
		{
            _context = context;
            _hubContext = hubContext;
            _logger = logger;
		}

		public async  Task<IActionResult> Index()
		{
			//Para obtener los datos de los claims personalizados
			if (User.Identity.IsAuthenticated)
			{
				var claimsIdentity = User.Identity as ClaimsIdentity;
				var userIdClaim = claimsIdentity.FindFirst("Id");
				ViewBag.User = userIdClaim.Value;
			}

			ViewBag.Loans = await _context.Loan.Take(5).ToListAsync();
			return View();
		}

		public ActionResult Privacy()
		{
			return View();
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public ActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
	}
}