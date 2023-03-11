using Library7.Data;
using Library7.Hubs;
using Library7.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
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

			var bookGroups = await _context.Book.Select(x => x.Group_Id)
				.Distinct().ToListAsync();

			List<Book> books = new();
			foreach (var bookGroup in bookGroups)
			{
				var book = await _context.Book
					.Where(x => x.Group_Id == bookGroup)
					.FirstOrDefaultAsync();
				books.Add(book);
			};
			ViewBag.Books = books;
			return View();
		}

		// When the user search for a book
		[HttpPost]
		public async Task<ActionResult> Index(string searchData)
		{
			var books = await _context.Book
				.Where(b => b.Title.Contains(searchData) || b.Author.Contains(searchData))
				.GroupBy(t => new { t.Group_Id, t.Title, t.Author, t.Id_Section, t.Image })
				.Select(r => new
				{
					r.Key.Group_Id,
					r.Key.Title,
					r.Key.Author,
					r.Key.Id_Section,
					r.Key.Image,
				})
				.ToListAsync();
			ViewBag.Books = (books.Count > 0) ? books : null;
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

			var books = await _context.Book
				.GroupBy(t => new { t.Group_Id, t.Title, t.Author, t.Id_Section, t.Image })
				.Select(r => new
				{
					r.Key.Group_Id,
					r.Key.Title,
					r.Key.Author,
					r.Key.Id_Section,
					r.Key.Image,
				})
				.Where(x => x.Id_Section == Id_Section)
				.ToListAsync();
			ViewBag.Books = (books.Count > 0) ? books : null;
			return View();
		}

		public async Task<ActionResult> BookInfo(int id)
		{
			if (id <= 0)
				return NotFound();

			var book = await _context.Book
				.Where(x => x.Group_Id == id)
				.FirstOrDefaultAsync();
			if (book == null)
				return NotFound();

			bool isSaved = false;
			var savedBook = await _context.SavedGroup
				.Where(x => x.Group_Id == id && x.Id_Member == GetMemberId())
				.FirstOrDefaultAsync();

			if (savedBook is not null)
				isSaved = true;

			ViewBag.isSaved = isSaved;
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

		[HttpGet]
		public async Task<ActionResult> SavedBooks()
		{
			int memberId = GetMemberId();

			var savedBooks = await _context.SavedGroup
				.Where(sb => sb.Id_Member == memberId)
				.Select(sb => new SavedGroupViewModel
				{
					Id_SavedGroup = sb.Id_SavedGroup,
					Id_Member= sb.Id_Member,
					Title = _context.Book
					.Where(b => b.Group_Id == sb.Group_Id)
					.Select(b => b.Title)
					.FirstOrDefault(),
				})
				.ToListAsync();
			return View(savedBooks);
		}
		#endregion

		#region Actions

		[HttpPost]
		public async Task<IActionResult> MakeSavedBook([Bind("Group_Id")] SavedGroup savedGroup)
		{
			int memberId = GetMemberId();

			if (!ModelState.IsValid || savedGroup.Group_Id <= 0)
				return NotFound(savedGroup);

			SavedGroup sav = new()
			{
				Group_Id = savedGroup.Group_Id,
				Id_Member = memberId
			};
			_context.Add(sav);
			await _context.SaveChangesAsync();
			return RedirectToAction("Index");
		}

		[HttpPost]
		public async Task<IActionResult> DeleteSavedBook([Bind("Id_SavedGroup")] SavedGroup savedBook)
		{
			if (savedBook.Id_SavedGroup <= 0 || !ModelState.IsValid)
				return NotFound();

			var sb = await _context.SavedGroup.FindAsync(savedBook.Id_SavedGroup);
			if (sb == null)
				return NotFound(sb);

			_context.SavedGroup.Remove(sb);
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
