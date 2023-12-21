using LibraryApp.Data;
using LibraryApp.Hubs;
using LibraryApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;

namespace LibraryApp.Controllers
{
	[Authorize]
	public class BookController : Controller
	{
		private readonly Library7Context _context;
		private readonly IHubContext<SignalRHub> _hubContext;
		private readonly IWebHostEnvironment _environment;


		public BookController(Library7Context context, IHubContext<SignalRHub> hubContext, IWebHostEnvironment environment)
		{
			_context = context;
			_hubContext = hubContext;
			_environment = environment;
		}

		#region Views
		[HttpGet]
		public ActionResult Index()
		{
			return View();
		}

		[HttpGet]
		public async Task<ActionResult> Details(int id)
		{
			if (id <= 0)
				return NotFound();
			var book = await _context.Book.FindAsync(id);
			if (book == null)
				return NotFound();
			return View(book);
		}

		[HttpGet]
		public async Task<ActionResult> Create()
		{
			var sections = await _context.Section.ToListAsync();
			ViewBag.Sections = new SelectList(sections, "Id_Section", "Name");
			return View();
		}

		[HttpGet]
		public async Task<ActionResult> CreateCopy(int id)
		{
			if (id <= 0)
				return NotFound();
			var book = await _context.Book.FindAsync(id);
			if (book == null)
				return NotFound();
			return View(book);
		}

		[HttpGet]
		public async Task<ActionResult> Edit(int id, int option)
		{
			if (id <= 0 || option < 0 || option > 1)
				return RedirectToAction(nameof(Index));

			var book = await _context.Book.FindAsync(id);
			if (book == null)
				return NotFound("The book doesnt exist");

			var sections = await _context.Section.ToListAsync();
			ViewBag.Sections = new SelectList(sections, "Id_Section", "Name");
			ViewBag.Option = option;
			return View(book);
		}

		[HttpGet]
		public async Task<ActionResult> Delete(int? id)
		{
			if (id == null || _context.Book == null)
				return NotFound();
			var libro = await _context.Book
				.FirstOrDefaultAsync(m => m.Id_Book == id);
			if (libro == null)
				return NotFound();
			return View(libro);
		}

		#endregion

		#region Actions

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(
			[Bind("Group_Id,ISBN,Title,Author,Id_Section,ImageFile")] Book book)
		{
			if (!ModelState.IsValid)
				return View(book);
			try
			{
				if (book.ImageFile != null && book.ImageFile.Length > 0)
				{
					await SaveImage(book.ImageFile);
					book.Image = book.ImageFile.FileName;
				}

				var maxGroupId = _context.Book.Max(x => x.Group_Id);
				if (book.Group_Id <= 0)
					book.Group_Id = maxGroupId + 1;
			}
			catch (Exception ex)
			{
				return NotFound(ex.Message);
			}

			_context.Add(book);

			await _context.SaveChangesAsync();
			await BookModConnection();
			return RedirectToAction(nameof(Index));
		}


		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(
			[Bind("Id_Book,ISBN,Title,Author,Id_Section,ImageFile")] Book book)
		{
			if (!ModelState.IsValid)
			{
				return View(book);
			}
				
			try
			{
				var existingBook = await _context.Book.FindAsync(book.Id_Book);
				if (existingBook == null)
					return NotFound("This book for some reason doesnt exists");
				if (book.ISBN == null) // Update of multiple records
				{
					await UpdateGroupOfBooks(existingBook, book);
				}
				else // Updae a single record
					existingBook.ISBN = book.ISBN;

				await _context.SaveChangesAsync();
				await BookModConnection();
			}
			catch (Exception ex)
			{
				return NotFound(ex);

			}
			return RedirectToAction(nameof(Index));
		}

		private async Task UpdateGroupOfBooks(Book existingBook, Book updatedBook)
		{
			//string? fileName = null;
			//if (updatedBook.ImageFile != null && updatedBook.ImageFile.Length > 0)
			//{
			//	if (existingBook.Image != null)
			//		await DeleteOldImage(existingBook.Image);

			//	//fileName = updatedBook.ImageFile.FileName;
			//	await SaveImage(updatedBook.ImageFile);
			//}

			var groupId = existingBook.Group_Id;
			var recordsToEdit = await _context.Book
				.Where(x => x.Group_Id == groupId)
				.ToListAsync();

			foreach (var record in recordsToEdit)
			{
				record.Author = updatedBook.Author;
				record.Title = updatedBook.Title;
				record.Id_Section = updatedBook.Id_Section;
				//record.Image = fileName;
				if (updatedBook.ImageFile != null && updatedBook.ImageFile.Length > 0)
				{
					if (record.Image != null)
						await DeleteOldImage(record.Image);

					record.Image = await SaveImage(updatedBook.ImageFile);
				}				
			}
		}



		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed([Bind("Id_Book")] Book book)
		{
			if (book.Id_Book <= 0)
				return BadRequest("Invalid book id.");

			var res = await _context.Book.FindAsync(book.Id_Book);
			if (res is null)
				return NotFound("Book not found");
			try
			{
				if(res.Image is not null)
					await DeleteOldImage(res.Image);

				_context.Book.Remove(res);
				await _context.SaveChangesAsync();				
				await BookModConnection();
			}
			catch (Exception ex)
			{
				return BadRequest(ex);
			}
			return RedirectToAction(nameof(Index));
		}

		#endregion

		public async Task<ActionResult> GetAll()
		{
			var books = await _context.Book
				.OrderBy(x => x.Group_Id)
				.ThenBy(x => x.Title)
				.ToListAsync();
			return Json(books);
		}

		private bool BookExists(int id)
		{
			return (_context.Book?.Any(e => e.Id_Book == id)).GetValueOrDefault();
		}
		// signalR
		private async Task BookModConnection() =>
			await _hubContext.Clients.All.SendAsync("BookModConnection");


		private async Task<string> SaveImage(IFormFile file)
		{
			// Get the file name and extension
			string fileName = Guid.NewGuid().ToString("N") + Path.GetExtension(file.FileName);
			//var fileName = Path.GetFileName(file.FileName);
			// Create a relative path to the folder in your project
			var filePath = Path.Combine(_environment.WebRootPath, "booksImages", fileName);
			// save the file to the specified path
			if (!System.IO.File.Exists(filePath))
			{
				using var fileStream = new FileStream(filePath, FileMode.Create);
				await file.CopyToAsync(fileStream);
			}
			return fileName;
		}

		private async Task DeleteOldImage(string oldFile)
		{			
			string oldImagePath = Path.Combine(_environment.WebRootPath, "booksImages", oldFile);
			if (System.IO.File.Exists(oldImagePath))
				System.IO.File.Delete(oldImagePath);			
		}
	}
}
