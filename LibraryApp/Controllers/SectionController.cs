using LibraryApp.Data;
using LibraryApp.Hubs;
using LibraryApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace LibraryApp.Controllers
{
	[Authorize]
	public class SectionController : Controller
	{
		private readonly Library7Context _context;
		private readonly IHubContext<SignalRHub> _hubContext;

		public SectionController(Library7Context context, IHubContext<SignalRHub> hubContext)
		{
			_context = context;
			_hubContext = hubContext;
		}

		#region Views
		[HttpGet]
		public ActionResult Index()
		{
			//return _context.Section != null ?
			//			 View(await _context.Section.ToListAsync()) :
			//			 Problem("Entity set 'Library7Context.Libro'  is null.");
			return View();
		}

		[HttpGet]
		public async Task<ActionResult> Details(int id)
		{
			if (id <= 0)
				return NotFound();

			var section = await _context.Section.FindAsync(id);
			if (section == null)
				return NotFound();

			return View(section);
		}

		[HttpGet]
		public ActionResult Create()
		{
			return View();
		}

		[HttpGet]
		public async Task<ActionResult> Edit(int id)
		{
			if (id <= 0)
				return NotFound();

			var section = await _context.Section.FindAsync(id);
			if (section == null)
				return NotFound();

			return View(section);
		}

		[HttpGet]
		public async Task<ActionResult> Delete(int id)
		{
			if (id <= 0)
				return NotFound();

			var section = await _context.Section.FindAsync(id);
			if (section == null)
				return NotFound();

			return View(section);
		}
		#endregion

		#region Actions
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(
			[Bind("Name")] Section section)
		{
			if (ModelState.IsValid)
			{
				_context.Add(section);
				await _context.SaveChangesAsync();
				await SectionModConnection();
				return RedirectToAction(nameof(Index));
			}
			return View(section);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(
			int id, [Bind("Id_Section,Name")] Section section)
		{
			if (id != section.Id_Section)
				return NotFound();

			if (ModelState.IsValid)
			{
				try
				{
					_context.Update(section);
					await _context.SaveChangesAsync();
					await SectionModConnection();
				}
				catch (DbUpdateConcurrencyException)
				{
					if (!BookExists(section.Id_Section))
						return NotFound();
					else
						throw;
				}
				return RedirectToAction(nameof(Index));
			}
			return View(section);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed([Bind("Id_Section")] Section sec)
		{
			if (sec.Id_Section <= 0)
				return NotFound(sec);

			var section = await _context.Section.FindAsync(sec.Id_Section);
			if (section == null)
				return NotFound(section);

			_context.Section.Remove(section);
			await _context.SaveChangesAsync();
			await SectionModConnection();

			return RedirectToAction(nameof(Index));
		}

		public async Task<JsonResult> GetAll()
		{
			var sections = await _context.Section.ToListAsync();
			return Json(sections);
		}
		#endregion

		private bool BookExists(int? id)
		{
			return (_context.Section?.Any(e => e.Id_Section == id)).GetValueOrDefault();
		}

		// signalR
		private async Task SectionModConnection() =>
			await _hubContext.Clients.All.SendAsync("SectionModConnection");
	}
}
