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
		private readonly IHubContext<SignalRHub> _hubContext;

		public MemberActionsController(
			Library7Context context, IHubContext<SignalRHub> hubContext)
		{
			_context = context;
			_hubContext = hubContext;
		}

		#region Views

		[HttpGet]
		public async Task<ActionResult> Index()
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

		// When the user searches for a book
		[HttpPost]
		public async Task<ActionResult> Index(string searchData)
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
		public async Task<ActionResult> Books(int Id_Section)
		{
			if (Id_Section <= 0)
				return View("Index");

			var section = await _context.Section
				.Where(x => x.Id_Section == Id_Section)
				.Select(x => x.Name)
				.FirstOrDefaultAsync();
			if (section == null)
				return View("Index");

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

		public async Task<ActionResult> BookInfo(int id)
		{
			if (id <= 0)
				return NotFound();

			var book = await _context.Book.FindAsync(id);
			if (book == null)
				return NotFound();

			return View(book);
		}

		public async Task<ActionResult> Loans()
		{
			int memberId = GetMemberId();

			var loans = await _context.Loan
				.Where(m => m.Id_Member == memberId)
				.ToListAsync();

			ViewBag.Loans = loans;
			ViewBag.MemberId = memberId;

			return View();
		}

		public async Task<ActionResult> SavedBooks()
		{
			int memberId = GetMemberId();

			var savedBooks = await _context.SavedBook
				.Where(x => x.Id_Member == memberId)
				.ToListAsync();
			return _context.SavedBook != null ?
						 View(savedBooks) :
						 Problem("Entity set 'Library7Context.Libro'  is null.");
		}
		#endregion

		#region Actions

		[HttpPost]
		public async Task<IActionResult> MakeSavedBook([Bind("Id_Book")] SavedBook savedBook)
		{
			int memberId = GetMemberId();

			if (!ModelState.IsValid || savedBook.Id_Book <= 0)
				return NotFound(savedBook);

			SavedBook sav = new()
			{
				Id_Book = savedBook.Id_Book,
				Id_Member = memberId
			};
			_context.Add(sav);
			await _context.SaveChangesAsync();
			return RedirectToAction("Index");
		}

		[HttpPost]
		public async Task<IActionResult> DeleteSavedBook([Bind("Id_SavedBook")] SavedBook savedBook)
		{
			if (savedBook.Id_SavedBook <= 0 || !ModelState.IsValid)
				return NotFound();

			var sb = await _context.SavedBook.FindAsync(savedBook.Id_SavedBook);
			if (sb == null)
				return NotFound(sb);

			_context.SavedBook.Remove(sb);
			await _context.SaveChangesAsync();

			return RedirectToAction(nameof(SavedBooks));
		}

		public int GetMemberId()
		{
			var claimsIdentity = User.Identity as ClaimsIdentity;
			var userIdClaim = claimsIdentity.FindFirst("Id");
			if (userIdClaim == null)
				return 0;
			return int.Parse(userIdClaim.Value);
		}

		#endregion




	}
}
