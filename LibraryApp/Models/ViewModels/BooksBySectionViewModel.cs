namespace LibraryApp.Models.ViewModels
{
	public class BooksBySectionViewModel
	{
		public string SectionName { get; set; }
		public List<Book>? Books { get; set;} 
	}
}
