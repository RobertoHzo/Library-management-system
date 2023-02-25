using Library7.Data;
using Library7.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library7.Controllers
{
	[Authorize]
	public class SectionController : Controller
    {
        private readonly Library7Context _context;
        //private readonly DBContext1 ;

        public SectionController(Library7Context context)
        {
            _context = context;
        }

        // GET: SectionController
        public async Task<IActionResult> Index()
        {
            return _context.Section != null ?
                         View(await _context.Section.ToListAsync()) :
                         Problem("Entity set 'Library7Context.Libro'  is null.");
        }


        // GET: SectionController/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Section == null)
            {
                return NotFound();
            }

            var section = await _context.Section
                .FirstOrDefaultAsync(m => m.Id_Section == id);
            if (section == null)
            {
                return NotFound();
            }

            return View(section);
        }

        // GET: SectionController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: SectionController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name")] Section section)
        {
            if (ModelState.IsValid)
            {
                _context.Add(section);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(section);
        }

        // GET: SectionController/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Section == null)
            {
                return NotFound();
            }

            var section = await _context.Section.FindAsync(id);
            if (section == null)
            {
                return NotFound();
            }
            return View(section);
        }

        // POST: SectionController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id_Section,Name")] Section section)
        {
            if (id != section.Id_Section)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(section);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookExists(section.Id_Section))
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
            return View(section);
        }

        // GET: SectionController/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Section == null)
            {
                return NotFound();
            }

            var section = await _context.Section
                .FirstOrDefaultAsync(m => m.Id_Section == id);
            if (section == null)
            {
                return NotFound();
            }

            return View(section);
        }

        // POST: SectionController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed()
        {
            string? formInput = Request.Form["Id_Section"];
            int? id = int.Parse(formInput);
            if (id != null)
            {
                if (_context.Section == null)
                {
                    return Problem("Entity set 'Library3Context.Libro'  is null.");
                }
                var section = await _context.Section.FindAsync(id);
                if (section != null)
                {
                    _context.Section.Remove(section);
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToAction(nameof(Index));
        }

        private bool BookExists(int? id)
        {
            return (_context.Section?.Any(e => e.Id_Section == id)).GetValueOrDefault();
        }
    }
}
