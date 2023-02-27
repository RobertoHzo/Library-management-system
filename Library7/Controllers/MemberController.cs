using Library7.Data;
using Library7.Hubs;
using Library7.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
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

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            return _context.Member != null ?
                         View(await _context.Member.ToListAsync()) :
                         Problem("Entity set 'Library7Context.Libro'  is null.");
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0)
                return NotFound();

            var member = await _context.Member.FindAsync(id);
            if (member == null)
                return NotFound();

            return View(member);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (id <= 0)
                return NotFound();

            var member = await _context.Member.FindAsync(id);
            if (member == null)
                return NotFound();

            return View(member);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, 
            [Bind("Id_Member,Name,Lastname,Email,Password,City,Address,Zip,Phone")] Member member)
        {
            if (id != member.Id_Member)            
                return NotFound();            

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(member);
                    await _context.SaveChangesAsync();
                    await MemeberModConnection();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MemberExists(member.Id_Member))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(member);
        }


        private async Task MemeberModConnection() =>
            await _hubContext.Clients.All.SendAsync("MemeberModConnection");
    }
}
