using Library7.Data;
using Library7.Hubs;
using Library7.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Library7.Controllers
{
    [Authorize]
    public class BookController : Controller
    {
        private readonly Library7Context _context;
        private readonly IHubContext<SignalRHub> _hubContext;

        public BookController(Library7Context context, IHubContext<SignalRHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            return _context.Book == null ?
                          Problem("Entity set 'Library7Context.Libro'  is null.") :
                          View(await _context.Book
                          .OrderBy(x => x.Group_Id)
                          .ThenBy(x => x.Title)
                          .ToListAsync());
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0)
                return NotFound();
            var book = await _context.Book.FindAsync(id);
            if (book == null)
                return NotFound();
            return View(book);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var sections = await _context.Section.ToListAsync();
            ViewBag.Sections = new SelectList(sections, "Id_Section", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Group_Id,ISBN,Title,Author,Id_Section,Image")] Book book)
        {
            if (!ModelState.IsValid)
                return View(book);
            var maxGroupId = _context.Book.Max(x => x.Group_Id);
            if (book.Group_Id <= 0)
                book.Group_Id = maxGroupId + 1;
            _context.Add(book);
            await _context.SaveChangesAsync();
            await BookModConnection();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> CreateCopy(int id)
        {
            if (id <= 0)
                return NotFound();
            var book = await _context.Book.FindAsync(id);
            if (book == null)
                return NotFound();
            return View(book);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, int option)
        {
            if (id <= 0)
                return NotFound();
            var book = await _context.Book.FindAsync(id);
            if (book == null)
                return NotFound();
            var sections = await _context.Section.ToListAsync();
            ViewBag.Sections = new SelectList(sections, "Id_Section", "Name");
            ViewBag.Option = option;
            return View(book);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("Id_Book,ISBN,Title,Author,Id_Section,Image")] Book book)
        {
            if (id != book.Id_Book)
                return NotFound();
            if (!ModelState.IsValid)
                return View(book);
            try
            {
                var existingBook = await _context.Book
                    .FirstOrDefaultAsync(x => x.Id_Book == id);
                if (existingBook == null)
                    return NotFound();
                if (book.ISBN == null) // Update of multiple records
                {
                    var groupId = existingBook.Group_Id;
                    var recordsToEdit = await _context.Book
                        .Where(x => x.Group_Id == groupId)
                        .ToListAsync();

                    foreach (var record in recordsToEdit)
                    {
                        record.Author = book.Author;
                        record.Title = book.Title;
                        record.Id_Section = book.Id_Section;
                        record.Image = book.Image;
                    }
                }
                else // Updae a single record
                    existingBook.ISBN = book.ISBN;
                await _context.SaveChangesAsync();
                await BookModConnection();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BookExists(book.Id_Book))
                    return NotFound();
                else
                    throw;
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Book == null)
                return NotFound();
            var libro = await _context.Book
                .FirstOrDefaultAsync(m => m.Id_Book == id);
            if (libro == null)
                return NotFound();
            return View(libro);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed([Bind("Id_Book")] Book book)
        {
            if (book.Id_Book <= 0)
                return BadRequest("Invalid book id.");
            var res = await _context.Book.FindAsync(book.Id_Book);
            if (res == null)
                return NotFound();
            _context.Book.Remove(res);
            await _context.SaveChangesAsync();
            await BookModConnection();
            return RedirectToAction(nameof(Index));
        }

        private bool BookExists(int id)
        {
            return (_context.Book?.Any(e => e.Id_Book == id)).GetValueOrDefault();
        }
        // signalR
        private async Task BookModConnection() =>
            await _hubContext.Clients.All.SendAsync("BookModConnection");
    }
}
