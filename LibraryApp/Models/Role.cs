using System.ComponentModel.DataAnnotations;

namespace LibraryApp.Models
{
    public class Role
    {
        [Key]
        public int Id_Role { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
