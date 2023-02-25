using Library7.Data;
using Library7.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Library7.Controllers
{
	[Authorize]
	public class BookController : Controller
	{
		private readonly Library7Context _context;
		//private readonly DBContext1 _dbContext;

		public BookController(Library7Context context)
		{
			_context = context;
			//_dbContext = dbContext;
		}
		// GET: BookController
		public async Task<IActionResult> Index()
		{
			return _context.Book != null ?
						  View(await _context.Book
						  .OrderBy(x => x.Group_Id)
						  .ThenBy(x => x.Title)
						  .ToListAsync()) :
						  Problem("Entity set 'Library7Context.Libro'  is null.");
		}

		// GET: BookController/Details/5
		public async Task<IActionResult> Details(int? id)
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

		// GET: BookController/Create
		[HttpGet]
		public async Task<IActionResult> Create()
		{
			var sections = await _context.Section.ToListAsync();
			ViewBag.Sections = new SelectList(sections, "Id_Section", "Name");

			return View();
		}

		// POST: BookController/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create([Bind("Group_Id,ISBN,Title,Author,Id_Section,Image")] Book book)
		{
			var MaxGroupId = _context.Book.Max(x => x.Group_Id);

			if (ModelState.IsValid)
			{
				if (book.Group_Id > 0)
				{
					_context.Add(book);
				}
				else
				{
					book.Group_Id = MaxGroupId + 1;
					_context.Add(book);
				}
				await _context.SaveChangesAsync();
				return RedirectToAction(nameof(Index));
			}
			return View(book);
		}

		[HttpGet]
		public async Task<IActionResult> CreateCopy(int id)
		{
			if (id == null || _context.Book == null)
			{
				return NotFound();
			}

			var book = await _context.Book.Where(x => x.Id_Book == id).FirstOrDefaultAsync();
			if (book == null)
			{
				return NotFound();
			}
			return View(book);
		}

		// GET: BookController/Edit/5
		[HttpGet]
		public async Task<IActionResult> Edit(int id, int option)
		{
			if (id == null || _context.Book == null)
			{
				return NotFound();
			}

			var sections = await _context.Section.ToListAsync();
			ViewBag.Sections = new SelectList(sections, "Id_Section", "Name");

			var book = await _context.Book.FindAsync(id);
			if (book == null)
			{
				return NotFound();
			}
			ViewBag.Option = option;
			return View(book);
		}

		// POST: BookController/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit([Bind("Id_Book,ISBN,Title,Author,Id_Section,Image")] Book book)
		{

			try
			{
				if (book.ISBN == null)
				{
					var groupId = await _context.Book
						.Where(x => x.Id_Book == book.Id_Book)
						.Select(y => y.Group_Id)
						.FirstOrDefaultAsync();

					var recordsToEdit = _context.Book.Where(x => x.Group_Id == groupId);

					foreach (var record in recordsToEdit)
					{
						record.Author = book.Author;
						record.Title = book.Title;
						record.Id_Section = book.Id_Section;
						record.Image = book.Image;
					}
				}
				else
				{
					var record = await _context.Book.FirstOrDefaultAsync(x => x.Id_Book == book.Id_Book);
					record.ISBN = book.ISBN;
				}
				await _context.SaveChangesAsync();
			}
			catch (DbUpdateConcurrencyException)
			{
				if (!BookExists(book.Id_Book))
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

		// GET: BookController/Delete/5
		public async Task<IActionResult> Delete(int? id)
		{
			if (id == null || _context.Book == null)
			{
				return NotFound();
			}

			var libro = await _context.Book
				.FirstOrDefaultAsync(m => m.Id_Book == id);
			if (libro == null)
			{
				return NotFound();
			}

			return View(libro);
		}

		// POST: BookController/Delete/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed()
		{
			string? formInput = Request.Form["Id_Book"];
			int? id = int.Parse(formInput);
			if (id != null)
			{
				if (_context.Book == null)
				{
					return Problem("Entity set 'Library3Context.Libro'  is null.");
				}
				var libro = await _context.Book.FindAsync(id);
				if (libro != null)
				{
					_context.Book.Remove(libro);
				}

				await _context.SaveChangesAsync();
			}
			return RedirectToAction(nameof(Index));
		}

		private bool BookExists(int id)
		{
			return (_context.Book?.Any(e => e.Id_Book == id)).GetValueOrDefault();
		}
	}
}
