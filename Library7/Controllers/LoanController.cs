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

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewBag.Config = await _context.LoanConfiguration.FirstOrDefaultAsync();
            var loans = await _context.Loan.ToListAsync();
            return View(loans);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0)
                return NotFound();
            var loan = await _context.Loan.FindAsync(id);
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
            if (!ModelState.IsValid)
                return View(loan);

            _context.Add(loan);
            await _context.SaveChangesAsync();
            await LoanModConnection();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id, [Bind("Id_Loan,Id_Book,Id_Member,LoanDate,DueDate,Finished")] Loan loan)
        {
            if (id != loan.Id_Loan)
                return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(loan);
                try
                {
                    await _context.SaveChangesAsync();
                    await LoanModConnection();
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
            return View(loan);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id <= 0)
                return NotFound();
            var loan = await _context.Loan.FindAsync(id);
            if (loan == null)
                return NotFound();

            return View(loan);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed([Bind("Id_Loan")] Loan loan)
        {
            int id = loan.Id_Loan;
            if (id <= 0)
            {
                var res = await _context.Loan.FindAsync(id);
                if (res != null)
                {
                    _context.Loan.Remove(res);
                    await _context.SaveChangesAsync();
                    await LoanModConnection();
                }
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
                    var data = await _context.Loan.FindAsync(floan.Id_Loan);
                    if (data != null)
                    {
                        var fa = await GetFineAmount(data.Id_Loan, DateTime.Now, _context);
                        data.ReturnDate = DateTime.Now;
                        data.Finished = true;
                        data.Fine = fa.Item1;
                        await _context.SaveChangesAsync();
                        await LoanModConnection();
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await LoanExists(floan.Id_Loan))
                        return NotFound();
                    else
                        throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<dynamic> ShowFineAmount(int Id_Loan)
        {
            if (Id_Loan <= 0) return null;

            DateTime returnDate = DateTime.Now;
            var fa = await GetFineAmount(Id_Loan, returnDate, _context);

            List<FineDay> jsonObject = new List<FineDay>
            {
                new FineDay{ Days = fa.Item2.ToString(), Fine = fa.Item1.ToString()}
            };

            return jsonObject;
        }

        //public async Task<(double?, int)> GetFineAmount(int Id_Loan, DateTime ReturnDate, Library7Context _context)
        //{
        //    int differenceInDays = 0;
        //    var loanConfig = await _context.LoanConfiguration.FirstOrDefaultAsync();
        //    var loan = await _context.Loan.FindAsync(Id_Loan);

        //    if (ReturnDate > DateTime.Now || ReturnDate < loan.DueDate)
        //        return (0, 0);

        //    if (loanConfig.Weekends == true)
        //    {
        //        TimeSpan difference = ReturnDate.Date - loan.DueDate;
        //        differenceInDays = difference.Days;
        //    }
        //    else
        //    {
        //        DateTime startDate = loan.DueDate;
        //        DateTime endDate = ReturnDate.Date;
        //        differenceInDays = 0;
        //        for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
        //        {
        //            if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
        //            {
        //                differenceInDays++;
        //            }
        //        }
        //    }
        //    var fine = differenceInDays * loanConfig.FineAmount;
        //    return (fine, differenceInDays);
        //}

        public async Task<(double FineAmount, int DifferenceInDays)> GetFineAmount(
            int loanId, DateTime returnDate, Library7Context context)
        {
            var loan = await context.Loan.FindAsync(loanId);
            if (loan == null)
                throw new ArgumentException($"Loan with id {loanId} not found", nameof(loanId));

            if (returnDate < loan.DueDate || returnDate > DateTime.Now)
                return (0, 0);

            var loanConfig = await context.LoanConfiguration.FirstOrDefaultAsync();
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
