using System.ComponentModel.DataAnnotations;

namespace LibraryApp.Models
{
	public class SavedGroup
	{
		[Key]
		public int Id_SavedGroup { get; set; }
		public int Id_Member { get; set; }
		public int Group_Id { get; set; }
	}
}
