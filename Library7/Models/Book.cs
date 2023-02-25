using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Library7.Models
{
    public class Book
    {
        [Key]
        public int Id_Book { get; set; }
		public string ISBN { get; set; }
		public string Title { get; set; }
        public string Author { get; set; }                
        public int Id_Section { get; set; }
        public string? Image { get; set; }
		public int Group_Id { get; set; }

		//[NotMapped]
		//public int Count { get; set; }

		// Verificacion para ver si se puede crear el prestamo
		//public ICollection<Loan> Loans { get; set; }
		//public bool CanLoan()
		//{
		//	int activeLoans = Loans.Count(l => l.ReturnDate == null);
		//	return activeLoans < Copies;
		//}

		//public int AvailableCopies
		//{
		//	get
		//	{
		//		int activeLoans = Loans.Count(l => l.ReturnDate == null);
		//		return Copies - activeLoans;
		//	}
		//}

		

	}
}
