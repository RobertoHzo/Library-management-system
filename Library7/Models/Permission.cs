using System.ComponentModel.DataAnnotations;

namespace Library7.Models
{
    public class Permission
    {
        [Key]
        public int Id_Permission { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
