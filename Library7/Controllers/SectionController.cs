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
    public class SectionController : Controller
    {
        private readonly Library7Context _context;
        private readonly IHubContext<SignalRHub> _hubContext;

        public SectionController(Library7Context context, IHubContext<SignalRHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            return _context.Section != null ?
                         View(await _context.Section.ToListAsync()) :
                         Problem("Entity set 'Library7Context.Libro'  is null.");
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0)
                return NotFound();

            var section = await _context.Section.FindAsync(id);
            if (section == null)
                return NotFound();

            return View(section);
        }

        [HttpGet]
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Name")] Section section)
        {
            if (ModelState.IsValid)
            {
                _context.Add(section);
                await _context.SaveChangesAsync();
                await SectionModConnection();
                return RedirectToAction(nameof(Index));
            }
            return View(section);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (id <= 0)
                return NotFound();

            var section = await _context.Section.FindAsync(id);
            if (section == null)
                return NotFound();

            return View(section);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id, [Bind("Id_Section,Name")] Section section)
        {
            if (id != section.Id_Section)            
                return NotFound();            

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(section);
                    await _context.SaveChangesAsync();
                    await SectionModConnection();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookExists(section.Id_Section))                    
                        return NotFound();                    
                    else                    
                        throw;                    
                }
                return RedirectToAction(nameof(Index));
            }
            return View(section);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)            
                return NotFound();

            var section = await _context.Section.FindAsync(id);
            if (section == null)            
                return NotFound();            

            return View(section);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed([Bind("Id_Section")]Section sec)
        {
            int id = sec.Id_Section;
            if (id <= 0)
            {
                var section = await _context.Section.FindAsync(id);
                if (section != null)
                {
                    _context.Section.Remove(section);
                    await _context.SaveChangesAsync();
                    await SectionModConnection();
                }
            }
            return RedirectToAction(nameof(Index));
        }

        private bool BookExists(int? id)
        {
            return (_context.Section?.Any(e => e.Id_Section == id)).GetValueOrDefault();
        }

        // signalR
        private async Task SectionModConnection() =>
            await _hubContext.Clients.All.SendAsync("SectionModConnection");
    }
}
