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
		private readonly Library7Context _context;
		private readonly IHubContext<SignalRHub> _hubContext;

		public LoanController(Library7Context context, IHubContext<SignalRHub> hubContext)
		{
			_context = context;
			_hubContext = hubContext;
		}

		#region Views
		[HttpGet]
		public async Task<ActionResult> Index()
		{
			ViewBag.Config = await _context.LoanConfiguration.SingleOrDefaultAsync();
			return View();
		}

		[HttpGet]
		public async Task<ActionResult> Details(int id)
		{
			if (id <= 0)
				return NotFound();
			var loan = await _context.Loan.FindAsync(id);
			if (loan == null)
				return NotFound();
			return View(loan);
		}

		[HttpGet]
		public async Task<ActionResult> Create()
		{
			var members = await _context.Member.ToListAsync();
			ViewBag.Members = new SelectList(members, "Id_Member", "Name");
			var books = await _context.Book.ToListAsync();
			ViewBag.Books = new SelectList(books, "Id_Book", "Title");
			return View();
		}
		[HttpGet]
		public async Task<ActionResult> Edit(int id)
		{
			if (id <= 0)
				return NotFound();

			var loan = await _context.Loan.FindAsync(id);
			if (loan == null)
				return NotFound();

			var members = await _context.Member.ToListAsync();
			ViewBag.Members = new SelectList(members, "Id_Member", "Name");
			var books = await _context.Book.ToListAsync();
			ViewBag.Books = new SelectList(books, "Id_Book", "ISBN");
			return View(loan);
		}

		[HttpGet]
		public async Task<ActionResult> Delete(int? id)
		{
			if (id <= 0)
				return NotFound();
			var loan = await _context.Loan.FindAsync(id);
			if (loan == null)
				return NotFound();

			return View(loan);
		}
		#endregion

		#region Actions
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(
			[Bind("Id_Book,Id_Member,LoanDate,DueDate,Finished")] Loan loan)
		{
			if (!ModelState.IsValid)
				return View(loan);

			_context.Add(loan);
			await _context.SaveChangesAsync();
			await LoanModConnection();
			await MemberLoan(loan.Id_Member);
			return RedirectToAction(nameof(Index));
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(
			int id, [Bind("Id_Loan,Id_Book,Id_Member,LoanDate,DueDate,Finished")] Loan loan)
		{
			if (id != loan.Id_Loan || id <= 0 || !ModelState.IsValid)
				return View(loan);

			try
			{
				_context.Update(loan);
				await _context.SaveChangesAsync();
				await LoanModConnection();
				await MemberLoan(loan.Id_Member);
			}
			catch (DbUpdateConcurrencyException)
			{
				if (!await LoanExists(loan.Id_Loan))
					return NotFound(loan);
				else
					throw;
			}
			return RedirectToAction(nameof(Index));
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed([Bind("Id_Loan")] Loan loan)
		{
			if (loan.Id_Loan <= 0 || !ModelState.IsValid)
				return NotFound(loan);

			var res = await _context.Loan.FindAsync(loan.Id_Loan);
			if (res == null)
				return NotFound(loan);

			_context.Loan.Remove(res);
			await _context.SaveChangesAsync();
			await LoanModConnection();
			return RedirectToAction(nameof(Index));
		}

		[HttpPost]
		public async Task<IActionResult> FinishLoan([Bind("Id_Loan")] Loan loan)
		{
			if (!ModelState.IsValid)
				return NotFound(loan);

			try
			{
				var data = await _context.Loan.FindAsync(loan.Id_Loan);
				if (data == null)
					return NotFound(loan);

				var (FineAmount, DifferenceInDays) = await GetFineAmount(data.Id_Loan, DateTime.Now, _context);
				data.ReturnDate = DateTime.Now;
				data.Finished = true;
				data.Fine = FineAmount;
				await _context.SaveChangesAsync();
				await LoanModConnection();
				await MemberLoan(loan.Id_Member);
			}
			catch (DbUpdateConcurrencyException)
			{
				if (!await LoanExists(loan.Id_Loan))
					return NotFound();
				else
					throw;
			}

			return RedirectToAction(nameof(Index));
		}

		public async Task<dynamic> ShowFineAmount(int Id_Loan)
		{
			if (Id_Loan <= 0) return null;

			DateTime returnDate = DateTime.Now;
			var (FineAmount, DifferenceInDays) = await GetFineAmount(Id_Loan, returnDate, _context);

			List<FineDay> jsonObject = new()
			{
				new FineDay{ Days = DifferenceInDays.ToString(), Fine = FineAmount.ToString()}
			};

			return jsonObject;
		}

		public async Task<(double FineAmount, int DifferenceInDays)> GetFineAmount(
			int loanId, DateTime returnDate, Library7Context context)
		{
			var loan = await context.Loan.FindAsync(loanId);
			if (loan == null)
				throw new ArgumentException($"Loan with id {loanId} not found", nameof(loanId));

			if (returnDate < loan.DueDate || returnDate > DateTime.Now)
				return (0, 0);

			var loanConfig = await context.LoanConfiguration.SingleOrDefaultAsync();
			int differenceInDays = CountBusinessDays(loan.DueDate, returnDate, loanConfig.Weekends);
			double fineAmount = differenceInDays * loanConfig.FineAmount;
			return (fineAmount, differenceInDays);
		}

		private static int CountBusinessDays(DateTime startDate, DateTime endDate, bool includeWeekends)
		{
			int days = 0;
			for (DateTime date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
			{
				if (!includeWeekends && (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday))
					continue;
				days++;
			}
			return days;
		}

		public async Task<ActionResult> EditLoanConfig(
			[Bind("Id,Weekends,FineAmount")] LoanConfiguration lc)
		{
			if (ModelState.IsValid)
			{
				var data = await _context.LoanConfiguration.FindAsync(lc.Id);
				if (data != null)
				{
					if (lc.Weekends == true)
						data.Weekends = true;
					else
						data.Weekends = false;

					data.FineAmount = lc.FineAmount;
					await _context.SaveChangesAsync();
					await LoanModConnection();
				}
			}
			return RedirectToAction("Index");
		}

		private async Task<bool> LoanExists(int id)
		{
			return await _context.Loan.AnyAsync(e => e.Id_Loan == id);
		}

		#endregion

		public async Task<IActionResult> GetAll()
		{
			var loans = await _context.Loan.Select(x => new
			{
				x.Id_Loan,
				x.Id_Book,
				x.Id_Member,
				LoanDate = x.LoanDate.ToString("dd/MM/yyyy"),
				DueDate = x.DueDate.ToString("dd/MM/yyyy"),
				ReturnDate = x.ReturnDate.HasValue ? x.ReturnDate.Value.ToString("dd/MM/yyyy hh:mm:ss") : "",
				x.Finished,
				x.Fine
			}).ToListAsync();
			return Json(loans);
		}


		// signalR
		private async Task LoanModConnection() =>
			await _hubContext.Clients.All.SendAsync("LoanModConnection");

		public async Task MemberLoan(int memberId)
		{
			await _hubContext.Clients.All.SendAsync("MemberLoan" + memberId);
		}

		#region objects
		//public class FinishLoanId
		//{
		//	public int Id_Loan { get; set; }
		//}
		public class FineDay
		{
			public string Days { get; set; }
			public string Fine { get; set; }
		}
		#endregion

	}
}
