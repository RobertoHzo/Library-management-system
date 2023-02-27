using Library7.Data;
using Library7.Hubs;
using Library7.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Claims;

namespace Library7.Controllers
{
    [Authorize]
    public class MemberActionsController : Controller
    {
        private readonly Library7Context _context;
        private readonly DBContext1 _dbContext;
        private readonly IHubContext<SignalRHub> _hubContext;

        public MemberActionsController(
            Library7Context context, DBContext1 dbContext, IHubContext<SignalRHub> hubContext)
        {
            _context = context;
            _dbContext = dbContext;
            _hubContext = hubContext;
        }

        #region Views

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewBag.Sections = await _context.Section.ToListAsync();

            ViewBag.Books = await _context.Book.GroupBy(t => new
            { t.Group_Id, t.Title, t.Author, t.Id_Section, t.Image })
                .Select(r => new
                {
                    Group_Id = r.Key.Group_Id,
                    Title = r.Key.Title,
                    Author = r.Key.Author,
                    Id_Section = r.Key.Id_Section,
                    Image = r.Key.Image,
                    Count = r.Count()
                })
                .ToListAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(string searchData)
        {
            var books = await _context.Book
                .Where(b => b.Title.Contains(searchData) || b.Author.Contains(searchData))
                .GroupBy(t => new { t.Group_Id, t.Title, t.Author, t.Id_Section, t.Image })
                .Select(r => new
                {
                    Group_Id = r.Key.Group_Id,
                    Title = r.Key.Title,
                    Author = r.Key.Author,
                    Id_Section = r.Key.Id_Section,
                    Image = r.Key.Image,
                    Count = r.Count()
                })
                .ToListAsync();
            ViewBag.Books = books;
            ViewBag.searchData = searchData;
            return View();
        }

        [HttpGet]
        [Route("MemberActions/Books/{Id_Section:int}")]
        public async Task<IActionResult> Books(int? Id_Section)
        {
            var section = await _context.Section
                .Where(x => x.Id_Section == Id_Section)
                .Select(x => x.Name)
                .FirstOrDefaultAsync();
            ViewBag.Section = section.ToString();

            ViewBag.Books = await _context.Book
                .GroupBy(t => new { t.Group_Id, t.Title, t.Author, t.Id_Section, t.Image })
                .Select(r => new
                {
                    Group_Id = r.Key.Group_Id,
                    Title = r.Key.Title,
                    Author = r.Key.Author,
                    Id_Section = r.Key.Id_Section,
                    Image = r.Key.Image,
                    Count = r.Count()
                })
                .Where(x => x.Id_Section == Id_Section)
                .ToListAsync();

            return View();
        }

        public async Task<IActionResult> BookInfo(int id)
        {
            if (id <= 0)
                return NotFound();

            var book = await _context.Book.FindAsync(id);
            if (book == null)
                return NotFound();

            return View(book);
        }

        public async Task<IActionResult> Loans()
        {
            var claimsIdentity = User.Identity as ClaimsIdentity;
            var userIdClaim = claimsIdentity.FindFirst("Id");

            var loans = await _context.Loan
                .Where(m => m.Id_Member == int.Parse(userIdClaim.Value))
                .ToListAsync();

            ViewBag.Loans = loans;

            return View();
        }

        public async Task<IActionResult> SavedBooks()
        {
            var claimsIdentity = User.Identity as ClaimsIdentity;
            var userIdClaim = claimsIdentity.FindFirst("Id");

            var savedBooks = await _context.SavedBook
                .Where(x => x.Id_Member == int.Parse(userIdClaim.Value))
                .ToListAsync();
            return _context.SavedBook != null ?
                         View(savedBooks) :
                         Problem("Entity set 'Library7Context.Libro'  is null.");
        }
        #endregion

        #region Actions

        [HttpGet]
        [Route("MemberActions/MakeSavedBook/{Id_Book:int}")]
        public async Task<IActionResult> MakeSavedBook(int Id_Book)
        {
            var claimsIdentity = User.Identity as ClaimsIdentity;
            var userIdClaim = claimsIdentity.FindFirst("Id");
            
            if(userIdClaim == null) return RedirectToAction("Login","Account");
            // Crea el SavedBook
            SavedBook sav = new SavedBook
            {
                Id_Book = Id_Book,
                Id_Member = int.Parse(userIdClaim.Value)
            };
            // Añade el SavedBook a la bd
            if (ModelState.IsValid)
            {
                _context.Add(sav);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(sav);
        }

        [HttpGet]
        [Route("MemberActions/DeleteSavedBook/{Id_SavedBook:int}")]
        public async Task<IActionResult> DeleteSavedBook(int Id_SavedBook)
        {
            if (Id_SavedBook <= 0)
            {
                var sb = await _context.SavedBook.FindAsync(Id_SavedBook);
                if (sb != null)
                {
                    _context.SavedBook.Remove(sb);
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToAction(nameof(SavedBooks));
        }

        #endregion

        #region Lists

        public async Task<IActionResult> GetBooksBySection(int Id_Section)
        {
            var booksList = await _context.Book
                .Where(b => b.Id_Section == Id_Section)
                .Select(b => new BookObject
                {
                    Id_Book = b.Id_Book.ToString(),
                    Title = b.Title,
                    Id_Section = b.Id_Section,
                    Image = b.Image
                })
                .ToListAsync();

            return Ok(booksList);
        }


        public async Task<IActionResult> GetSections()
        {
            var sectionList = await _context.Section.ToListAsync();
            return Ok(sectionList);
        }


        #endregion

        #region Objects
        public class BookObject
        {
            public string Id_Book { get; set; }
            public string Title { get; set; }
            public int Id_Section { get; set; }
            public string? Image { get; set; }
        }
        #endregion

    }
}
