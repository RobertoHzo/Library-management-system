using Library7.Data;
using Library7.Hubs;
using Library7.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.EntityFrameworkCore;

namespace Library7.Controllers
{
    [Authorize]
    public class MemberController : Controller
    {
        private readonly Library7Context _context;
        private readonly IHubContext<SignalRHub> _hubContext;

        public MemberController(Library7Context context, IHubContext<SignalRHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

		#region Views
		[HttpGet]
        public ActionResult Index()
        {
           return View();
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpGet]
        public async Task<ActionResult> Details(int id)
        {
            if (id <= 0)
                return NotFound();

            var member = await _context.Member.FindAsync(id);
            if (member == null)
                return NotFound();

            return View(member);
        }

        [HttpGet]
        public async Task<ActionResult> Edit(int id)
        {
            if (id <= 0)
                return NotFound();

            var member = await _context.Member.FindAsync(id);
            if (member == null)
                return NotFound();

            return View(member);
        }

        [HttpGet]
        public async Task<ActionResult> Delete(int id)
        {
            if (id <= 0)
                return NotFound();

            var member = await _context.Member.FindAsync(id);
            if (member == null)
                return NotFound();

            return View(member);
        }
        #endregion

        #region Actions
        public async Task<IActionResult> Create(
            [Bind("Name,Lastname,Email,Password,Phone,City,Address,Zip")] Member member)
        {
            if (!ModelState.IsValid)
                return View(member);

            _context.Add(member);
            await _context.SaveChangesAsync();
            await MemberModConnection();
            return RedirectToAction(nameof(Index));

        }

        [HttpPost]
        public async Task<IActionResult> Edit(
            [Bind("Id_Member,Name,Lastname,Email,Password,Phone,City,Address,Zip")] Member member)
        {
            if (member.Id_Member <= 0 || !ModelState.IsValid)
                return View(member);
            _context.Add(member);
            await _context.SaveChangesAsync();
            await MemberModConnection();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteConfirmed([Bind("Id_Member")] Member member)
        {
            if (member.Id_Member <= 0)
                return NotFound();

            var res = await _context.Member.FindAsync(member.Id_Member);
            if (res == null)
                return NotFound();

            _context.Member.Remove(res);
            await _context.SaveChangesAsync();
            await MemberModConnection();
            return RedirectToAction(nameof(Index));
        }
        #endregion

        public async Task<IActionResult> GetAll()
        {
            var members = await _context.Member.ToListAsync();
			return Json(members);
        }
             

        private async Task MemberModConnection() =>
            await _hubContext.Clients.All.SendAsync("MemberModConnection");
    }
}
