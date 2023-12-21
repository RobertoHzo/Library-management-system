namespace LibraryApp.Models.ViewModels
{
    public class SavedGroupViewModel
    {
        public int Id_SavedGroup { get; set; }
        public int Id_Member { get; set; }
        public int Group_Id { get; set; }
        public string? Image { get; set; } = default!;
        public string Title { get; set; } = default!;
    }
}
