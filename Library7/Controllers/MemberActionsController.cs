using Library7.Data;
using Microsoft.AspNetCore.Mvc;
using Library7.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Data;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using System.Security.Claims;
using Microsoft.Identity.Client;
using Microsoft.AspNetCore.Authorization;

namespace Library7.Controllers
{
	[Authorize]
	public class MemberActionsController : Controller
	{
		private readonly Library7Context _context;
		private readonly DBContext1 _dbContext;

		public MemberActionsController(Library7Context context, DBContext1 dbContext)
		{
			_context = context;
			_dbContext = dbContext;
		}

		#region Views

		[HttpGet]
		public async Task<IActionResult> Index()
		{
			ViewBag.Sections = await _context.Section.ToListAsync();

			ViewBag.Books = await _context.Book.GroupBy(t => new { t.Group_Id, t.Title, t.Author, t.Id_Section, t.Image })
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
			//var books = await _context.Book
			//.Where(b => b.Title.Contains(searchData) || b.ISBN.Contains(searchData))
			//.ToListAsync();
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
			if (id == null || _context.Book == null)
			{
				return NotFound();
			}

			var book = await _context.Book
				.FirstOrDefaultAsync(m => m.Id_Book == id);
			if (book == null)
			{
				return NotFound();
			}

			return View(book);
		}

		public async Task<IActionResult> Loans()
		{
			var claimsIdentity = User.Identity as ClaimsIdentity;
			var userIdClaim = claimsIdentity.FindFirst("Id");

			var loan = await _context.Loan.Where(m => m.Id_Member == int.Parse(userIdClaim.Value)).ToListAsync();
			ViewBag.Loans = loan;

			
			return View();
		}

		public async Task<IActionResult> SavedBooks()
		{
			var claimsIdentity = User.Identity as ClaimsIdentity;
			var userIdClaim = claimsIdentity.FindFirst("Id");

			return _context.SavedBook != null ?
						 View(await _context.SavedBook.Where(x => x.Id_Member == int.Parse(userIdClaim.Value)).ToListAsync()) :
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
			if (Id_SavedBook != null)
			{
				if (_context.SavedBook == null)
				{
					return Problem("Entity set 'Library3Context.SavedBook'  is null.");
				}
				var sb = await _context.SavedBook.FindAsync(Id_SavedBook);
				if (sb != null)
				{
					_context.SavedBook.Remove(sb);
				}
				await _context.SaveChangesAsync();
			}

			return RedirectToAction(nameof(SavedBooks));
		}

		#endregion

		#region Lists

		public async Task<IActionResult> GetBooksBySection(int Id_Section)
		{
			//List<Book> booksList = new List<Book>();
			List<BookObject> booksList = new List<BookObject>();

			using (SqlConnection con = new(_dbContext.Valor))
			{
				using (SqlCommand cmd = new("sp_BooksBySection", con))
				{
					cmd.CommandType = CommandType.StoredProcedure;
					cmd.Parameters.Add("@P1", SqlDbType.VarChar).Value = Id_Section;
					con.Open();

					SqlDataReader reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						BookObject book = new BookObject
						{
							Id_Book = reader["Id_Book"].ToString(),
							Title = reader["Title"].ToString(),
							Id_Section = Convert.ToInt32(reader["Id_Section"]),
							Image = reader["Image"].ToString(),
						};
						booksList.Add(book);
					}
					reader.Close();
					con.Close();
				}
			}
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
			public string Image { get; set; }
		}
		#endregion

	}
}
