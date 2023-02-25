using System.ComponentModel.DataAnnotations;

namespace Library7.Models
{
    public class Role
    {
        [Key]
        public int Id_Role { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
