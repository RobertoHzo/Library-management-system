using System.ComponentModel.DataAnnotations;

namespace LibraryApp.Models
{
    public class Permission
    {
        [Key]
        public int Id_Permission { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
