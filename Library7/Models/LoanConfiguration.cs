using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Library7.Models
{
	public class LoanConfiguration
	{
		[Key]
		public int Id { get; set; }
		public bool? Weekends { get; set; }
		public double? FineAmount { get; set; }
	}
}
