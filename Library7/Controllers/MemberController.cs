using Library7.Data;
using Library7.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library7.Controllers
{
	[Authorize]
	public class MemberController : Controller
    {
        private readonly Library7Context _context;
        //private readonly DBContext1 ;

        public MemberController(Library7Context context)
        {
            _context = context;
        }

        // GET: MemberController
        public async Task<IActionResult> Index()
        {
            return _context.Member != null ?
                         View(await _context.Member.ToListAsync()) :
                         Problem("Entity set 'Library7Context.Libro'  is null.");
        }


        // GET: MemberController/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Member == null)
            {
                return NotFound();
            }

            var member = await _context.Member
                .FirstOrDefaultAsync(m => m.Id_Member == id);
            if (member == null)
            {
                return NotFound();
            }

            return View(member);
        }

        // GET: MemberController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: MemberController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Lastname,Email,Password,City,Address,Zip,Phone")] Member member)
        {
            if (ModelState.IsValid)
            {
                _context.Add(member);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(member);
        }

        // GET: MemberController/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Member == null)
            {
                return NotFound();
            }

            var member = await _context.Member.FindAsync(id);
            if (member == null)
            {
                return NotFound();
            }
            return View(member);
        }

        // POST: MemberController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id_Member,Name,Lastname,Email,Password,City,Address,Zip,Phone")] Member member)
        {
            if (id != member.Id_Member)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(member);
                    await _context.SaveChangesAsync();
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

        // GET: MemberController/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Member == null)
            {
                return NotFound();
            }

            var member = await _context.Member
                .FirstOrDefaultAsync(m => m.Id_Member == id);
            if (member == null)
            {
                return NotFound();
            }

            return View(member);
        }

        // POST: MemberController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed()
        {
            string? formInput = Request.Form["Id_Member"];
            int? id = int.Parse(formInput);
            if (id != null)
            {
                if (_context.Member == null)
                {
                    return Problem("Entity set 'Library3Context.Libro'  is null.");
                }
                var member = await _context.Member.FindAsync(id);
                if (member != null)
                {
                    _context.Member.Remove(member);
                }

                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));

        }
        private bool MemberExists(int id)
        {
            return (_context.Member?.Any(e => e.Id_Member == id)).GetValueOrDefault();
        }
    }
}
