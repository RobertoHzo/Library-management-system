using LibraryApp.Data;
using LibraryApp.Hubs;
using LibraryApp.Models;
using LibraryApp.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Claims;

namespace LibraryApp.Controllers
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
			var sections = await _context.Section.ToListAsync();

			var bookGroups = await _context.Book
				.Select(b => b.Group_Id)
				.Distinct()
				.OrderBy(_ => Guid.NewGuid())
				.Take(10)
				.ToListAsync();

			var books = await _context.Book
				.Where(b => bookGroups.Contains(b.Group_Id))
				.Select(b => new BookViewModel
				{
					Title = b.Title,
					Image = b.Image,
					Author = b.Author,
					Group_Id = b.Group_Id
				})
				.Distinct()
				.ToListAsync();

			var viewModel = new IndexViewModel
			{
				Sections = sections,
				Books = books
			};

			return View(viewModel);
		}
		[HttpGet]
		public async Task<ActionResult> Books()
		{
			ViewBag.Books = await _context.Book
				.GroupBy(t => new { t.Group_Id, t.Title, t.Author, t.Id_Section, t.Image })
				.Select(g => new BookViewModel
				{
					Group_Id = g.Key.Group_Id,
					Title = g.Key.Title,
					Author = g.Key.Author,
					Id_Section = g.Key.Id_Section,
					Image = g.Key.Image
				})
				.ToListAsync();
			return View();
		}
		// When the user search a book
		[HttpPost]
		public async Task<ActionResult> Books(string searchData)
		{
			var query = _context.Book
				.Where(b => b.Title.Contains(searchData) || b.Author.Contains(searchData))
				.GroupBy(b => new { b.Group_Id, b.Title, b.Author, b.Id_Section, b.Image })
				.Select(g => new BookViewModel
				{
					Group_Id = g.Key.Group_Id,
					Title = g.Key.Title,
					Author = g.Key.Author,
					Id_Section = g.Key.Id_Section,
					Image = g.Key.Image
				});

			ViewBag.Books = await query.ToListAsync();

			ViewBag.searchData = searchData;
			return View();
		}

		[HttpGet]
		public async Task<ActionResult> Section(int id)
		{
			if (id <= 0)
				return View("Index");

			var section = await _context.Section
				.Where(x => x.Id_Section == id)
				.Select(x => x.Name)
				.FirstOrDefaultAsync();

			if (section is null)
				return View("Index");

			ViewBag.Section = section.ToString();

			var books = await _context.Book
				.Where(b => b.Id_Section == id)
				.Distinct()
				.ToListAsync();

			var model = new BooksBySectionViewModel
			{
				SectionName = section,
				Books = (books is not null) ? books : null
			};

			return View(model);
		}

		public async Task<IActionResult> BookInfo(int id)
		{
			if (id <= 0)
				return NotFound();

			var book = await _context.Book
				.FirstOrDefaultAsync(x => x.Group_Id == id);

			if (book is null)
				return NotFound();

			bool isSaved = await _context.SavedGroup
				.AnyAsync(x => x.Group_Id == id && x.Id_Member == GetMemberId());

			ViewBag.isSaved = isSaved && isSaved;

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
					Id_Member = sb.Id_Member,				
					Group_Id = sb.Group_Id,
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
