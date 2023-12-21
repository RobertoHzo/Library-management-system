using System.ComponentModel.DataAnnotations;

namespace Library7.Models
{
    public class Section
    {
        [Key]
        public int Id_Section { get; set; }
        public string Name { get; set; }
    }
}
