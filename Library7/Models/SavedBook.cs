using System.ComponentModel.DataAnnotations;

namespace Library7.Models
{
	public class SavedBook
	{
		[Key]
		public int Id_SavedBook { get; set; }
		public int Id_Book { get; set; }
		public int Id_Member { get; set; }
	}
}
