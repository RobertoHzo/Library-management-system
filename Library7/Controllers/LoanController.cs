using Library7.Data;
using Library7.Hubs;
using Library7.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;

namespace Library7.Controllers
{
	[Authorize]
	public class LoanController : Controller
	{
		//private readonly SignalRHub _hubInstance;

		private readonly Library7Context _context;
		private readonly IHubContext<SignalRHub> _hubContext;


		public LoanController(Library7Context context, IHubContext<SignalRHub> hubContext)
		{
			_context = context;
			_hubContext = hubContext;

		}

		[HttpGet]
		public async Task<IActionResult> Index()
		{
			ViewBag.Config = await _context.LoanConfiguration.FirstOrDefaultAsync();
			var loans = await (from a in _context.Loan select a).ToListAsync();
			return View(loans);
		}

		[HttpGet]
		public async Task<IActionResult> Details(int id)
		{
			if (id == null || _context.Loan == null)
				return NotFound();

			var loan = await _context.Loan
				.FirstOrDefaultAsync(m => m.Id_Loan == id);
			if (loan == null)
				return NotFound();

			return View(loan);
		}

		[HttpGet]
		public async Task<IActionResult> Create()
		{
			var members = await _context.Member.ToListAsync();
			ViewBag.Members = new SelectList(members, "Id_Member", "Name");
			var books = await _context.Book.ToListAsync();
			ViewBag.Books = new SelectList(books, "Id_Book", "Title");
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(
			[Bind("Id_Book,Id_Member,LoanDate,DueDate,Finished")] Loan loan)
		{
			if (ModelState.IsValid)
			{
				_context.Add(loan);
				await _context.SaveChangesAsync();
				await LoanModConnection();
				return RedirectToAction(nameof(Index));
			}
			return View(loan);
		}

		[HttpGet]
		public async Task<IActionResult> Edit(int? id)
		{
			if (id == null || _context.Loan == null)
				return NotFound();

			var members = await _context.Member.ToListAsync();
			ViewBag.Members = new SelectList(members, "Id_Member", "Name");
			var books = await _context.Book.ToListAsync();
			ViewBag.Books = new SelectList(books, "Id_Book", "ISBN");

			var loan = await _context.Loan.FindAsync(id);
			if (loan == null)
				return NotFound();

			return View(loan);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(
			int id, [Bind("Id_Loan,Id_Book,Id_Member,LoanDate,DueDate,Finished")] Loan loan)
		{
			// TODO
			if (id != loan.Id_Loan)
				return NotFound();
			if (ModelState.IsValid)
			{
				try
				{
					_context.Update(loan);
					await LoanModConnection();
				}
				catch (DbUpdateConcurrencyException)
				{
					if (!await LoanExists(loan.Id_Loan))
						return NotFound();
					else
					{
						throw;
					}
				}
				return RedirectToAction(nameof(Index));
			}
			return View(loan);
		}

		[HttpGet]
		public async Task<IActionResult> Delete(int? id)
		{
			if (id == null || _context.Loan == null)
				return NotFound();

			var loan = await _context.Loan
				.FirstOrDefaultAsync(m => m.Id_Loan == id);
			if (loan == null)
				return NotFound();

			return View(loan);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed()
		{
			string? formInput = Request.Form["Id_Loan"];
			int? id = int.Parse(formInput);
			if (id != null)
			{
				if (_context.Loan == null)
				{
					return Problem("Null");
				}
				var libro = await _context.Loan.FindAsync(id);
				if (libro != null)
				{
					_context.Loan.Remove(libro);
				}
				await _context.SaveChangesAsync();

				await LoanModConnection();
			}
			return RedirectToAction(nameof(Index));
		}

		[HttpPost]
		public async Task<IActionResult> FinishLoan([Bind("Id_Loan")] FinishLoanId floan)
		{
			if (ModelState.IsValid)
			{
				try
				{
					var data = await _context.Loan
						.Where(x => x.Id_Loan == floan.Id_Loan)
						.FirstOrDefaultAsync();
					var fa = await GetFineAmount(data.Id_Loan, DateTime.Now, _context);
					if (data != null)
					{
						data.ReturnDate = DateTime.Now;
						data.Finished = true;
						data.Fine = fa.Item1;
						await _context.SaveChangesAsync();
					}
				}
				catch (DbUpdateConcurrencyException)
				{
					if (!await LoanExists(floan.Id_Loan))
					{
						return NotFound();
					}
					else
					{
						throw;
					}
				}
			}
			return RedirectToAction(nameof(Index));
		}

		public async Task<dynamic> ShowFineAmount(int Id_Loan)
		{
			DateTime returnDate = DateTime.Now;
			var fa = await GetFineAmount(Id_Loan, returnDate, _context);

			List<FineDay> jsonObject = new List<FineDay>
			{
				new FineDay{ Days = fa.Item2.ToString(), Fine = fa.Item1.ToString()}
			};

			return jsonObject;
		}

		public async Task<(double?, int)> GetFineAmount(int Id_Loan, DateTime ReturnDate, Library7Context _context)
		{
			int differenceInDays = 0;
			var loanConfig = await _context.LoanConfiguration.FirstOrDefaultAsync();
			var loan = _context.Loan
				.Where(x => x.Id_Loan == Id_Loan).FirstOrDefault();
			if (ReturnDate > DateTime.Now || ReturnDate < loan.DueDate)
			{
				return (0, 0);
			}

			if (loanConfig.Weekends == true)
			{
				TimeSpan difference = ReturnDate.Date - loan.DueDate;
				differenceInDays = difference.Days;
			}
			else
			{
				DateTime startDate = loan.DueDate;
				DateTime endDate = ReturnDate.Date;
				differenceInDays = 0;
				for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
				{
					if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
					{
						differenceInDays++;
					}
				}
			}
			var fine = differenceInDays * loanConfig.FineAmount;
			return (fine, differenceInDays);
		}


		public async Task<ActionResult> EditLoanConfig(
			[Bind("Id,Weekends,FineAmount")] LoanConfiguration lc)
		{
			if (ModelState.IsValid)
			{
				var data = await _context.LoanConfiguration
					.Where(x => x.Id == lc.Id).FirstOrDefaultAsync();
				if (data != null)
				{
					if (lc.Weekends == true)
					{
						data.Weekends = true;
					}
					else
					{
						data.Weekends = false;
					}
					data.FineAmount = lc.FineAmount;
					await _context.SaveChangesAsync();
				}
			}
			return RedirectToAction("Index");
		}

		private async Task<bool> LoanExists(int id)
		{
			return await _context.Loan.AnyAsync(e => e.Id_Loan == id);
		}

		private async Task LoanModConnection() => 
			await _hubContext.Clients.All.SendAsync("LoanModification");

		#region objects
		public class FinishLoanId
		{
			public int Id_Loan { get; set; }
		}
		public class FineDay
		{
			public string Days { get; set; }
			public string Fine { get; set; }
		}
		#endregion

	}
}
