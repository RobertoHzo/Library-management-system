using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryApp.Models
{
    public class Book
    {
        [Key]
        public int Id_Book { get; set; }
        public string? ISBN { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public int Id_Section { get; set; }
        public string? Image { get; set; }
        public int Group_Id { get; set; }

        [NotMapped]
		public IFormFile? ImageFile { get; set; }
	}
}
