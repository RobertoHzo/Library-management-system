using Library7.Data;
using Library7.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library7.Controllers
{
	[Authorize]
	public class FineController : Controller
    {
        private readonly Library7Context _context;
        //private readonly DBContext1 ;

        public FineController(Library7Context context)
        {
            _context = context;
        }

        // GET: FineController
        public async Task<IActionResult> Index()
        {
            return _context.Fine != null ?
                         View(await _context.Fine.ToListAsync()) :
                         Problem("Entity set 'Library7Context.Libro'  is null.");
        }


        // GET: FineController/Details/5
        public async Task<IActionResult> Details(int id)
        {
            if (id == null || _context.Fine == null)
            {
                return NotFound();
            }

            var fine = await _context.Fine
                .FirstOrDefaultAsync(m => m.Id_Fine == id);
            if (fine == null)
            {
                return NotFound();
            }

            return View(fine);
        }

        // GET: FineController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: FineController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Amount,Id_Loan")] Fine fine)
        {
            if (ModelState.IsValid)
            {
                _context.Add(fine);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(fine);
        }

        // GET: FineController/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            if (id == null || _context.Fine == null)
            {
                return NotFound();
            }

            var fine = await _context.Fine.FindAsync(id);
            if (fine == null)
            {
                return NotFound();
            }
            return View(fine);
        }

        // POST: FineController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id_Fine,Amount,Id_Loan")] Fine fine)
        {
            if (id != fine.Id_Fine)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(fine);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FineExists(fine.Id_Fine))
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
            return View(fine);
        }

        // GET: FineController/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            if (id == null || _context.Fine == null)
            {
                return NotFound();
            }

            var fine = await _context.Fine
                .FirstOrDefaultAsync(m => m.Id_Fine == id);
            if (fine == null)
            {
                return NotFound();
            }

            return View(fine);
        }

        // POST: FineController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed()
        {
            string formInput = Request.Form["Id_Fine"];
            int? id = int.Parse(formInput);
            if (id != null)
            {
                if (_context.Fine == null)
                {
                    return Problem("Entity set 'Library3Context.Libro'  is null.");
                }
                var fine = await _context.Fine.FindAsync(id);
                if (fine != null)
                {
                    _context.Fine.Remove(fine);
                }

                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));

        }
        private bool FineExists(int id)
        {
            return (_context.Fine?.Any(e => e.Id_Fine == id)).GetValueOrDefault();
        }
    }
}
