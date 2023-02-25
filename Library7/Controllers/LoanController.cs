using Library7.Data;
using Library7.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Library7.Controllers
{
	[Authorize]
	public class LoanController : Controller
	{
		private readonly Library7Context _context;
		//private readonly DBContext1 ;

		public LoanController(Library7Context context)
		{
			_context = context;
		}

		[HttpGet]
		public async Task<IActionResult> Index()
		{
			ViewBag.Config = _context.LoanConfiguration.FirstOrDefault();
			var loans = await (from a in _context.Loan select a).ToListAsync();
			return View(loans);
		}

		[HttpGet]
		public async Task<IActionResult> Details(int id)
		{
			if (id == null || _context.Loan == null)
			{
				return NotFound();
			}

			var loan = await _context.Loan
				.FirstOrDefaultAsync(m => m.Id_Loan == id);
			if (loan == null)
			{
				return NotFound();
			}

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
		public async Task<IActionResult> Create([Bind("Id_Book,Id_Member,LoanDate,DueDate,Finished")] Loan loan)
		{
			if (ModelState.IsValid)
			{
				_context.Add(loan);
				await _context.SaveChangesAsync();
				return RedirectToAction(nameof(Index));
			}
			return View(loan);

			//if (ModelState.IsValid)
			//{
			//	if (!_context.Book.Find(loan.Id_Book).CanLoan())
			//	{
			//		return BadRequest("No hay suficientes copias disponibles para prestar este libro.");
			//	}
			//	_context.Loan.Add(loan);
			//	_context.SaveChanges();
			//}
			//return View(loan);
		}

		[HttpGet]
		public async Task<IActionResult> Edit(int? id)
		{
			if (id == null || _context.Loan == null)
			{
				return NotFound();
			}
			var members = await _context.Member.ToListAsync();
			ViewBag.Members = new SelectList(members, "Id_Member", "Name");
			var books = await _context.Book.ToListAsync();
			ViewBag.Books = new SelectList(books, "Id_Book", "ISBN");

			var loan = await _context.Loan.FindAsync(id);
			if (loan == null)
			{
				return NotFound();
			}
			return View(loan);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, [Bind("Id_Loan,Id_Book,Id_Member,LoanDate,DueDate,Finished")] Loan loan)
		{
			if (id != loan.Id_Loan)
			{
				return NotFound();
			}

			if (ModelState.IsValid)
			{
				try
				{
					_context.Update(loan);
					await _context.SaveChangesAsync();
				}
				catch (DbUpdateConcurrencyException)
				{
					if (!LoanExists(loan.Id_Loan))
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
			return View(loan);
		}

		[HttpGet]
		public async Task<IActionResult> Delete(int? id)
		{
			if (id == null || _context.Loan == null)
			{
				return NotFound();
			}

			var libro = await _context.Loan
				.FirstOrDefaultAsync(m => m.Id_Loan == id);
			if (libro == null)
			{
				return NotFound();
			}

			return View(libro);
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
					return Problem("Entity set 'Library3Context.Libro'  is null.");
				}
				var libro = await _context.Loan.FindAsync(id);
				if (libro != null)
				{
					_context.Loan.Remove(libro);
				}
				await _context.SaveChangesAsync();
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

					var data = await _context.Loan.Where(x => x.Id_Loan == floan.Id_Loan).FirstOrDefaultAsync();
					var fa = GetFineAmount(data.Id_Loan, DateTime.Now, _context);
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
					if (!LoanExists(floan.Id_Loan))
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
			//DateTime returnDate= DateTime.Parse(ReturnDate);
			DateTime returnDate = DateTime.Now;
			var fa = GetFineAmount(Id_Loan, returnDate,_context);

			List<FineDay> jsonObject= new List<FineDay> 
			{ 
				new FineDay{ Days = fa.Item2.ToString(), Fine = fa.Item1.ToString()}
			};

			return jsonObject;
		}
		public static (double?, int) GetFineAmount(int Id_Loan, DateTime ReturnDate, Library7Context _context)
		{
			int differenceInDays = 0;
			var loanConfig = _context.LoanConfiguration.FirstOrDefault();
			var loan = _context.Loan.Where(x => x.Id_Loan == Id_Loan).FirstOrDefault();
			//DateTime returnDate = DateTime.Parse(ReturnDate);
			if (ReturnDate > DateTime.Now || ReturnDate < loan.DueDate)
			{
				return (0, 0);
			}
			
			if (loanConfig.Weekends == true)
			{
				TimeSpan difference = ReturnDate.Date - loan.DueDate; // Get the difference in days
				differenceInDays = difference.Days; // Get the difference in days as an integer
			}
			else
			{
				DateTime date1 = loan.DueDate; // The start date
				DateTime date2 = ReturnDate.Date; // The end date
				differenceInDays = 0;
				for (DateTime date = date1; date <= date2; date = date.AddDays(1))
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


		public ActionResult EditLoanConfig([Bind("Id,Weekends,FineAmount")] LoanConfiguration lc)
		{
			if (ModelState.IsValid)
			{
				var data = _context.LoanConfiguration.Where(x => x.Id == lc.Id).FirstOrDefault();
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
					_context.SaveChanges();
				}
			}
			return RedirectToAction("Index");
		}

		private bool LoanExists(int id)
		{
			return (_context.Loan?.Any(e => e.Id_Loan == id)).GetValueOrDefault();
		}

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
