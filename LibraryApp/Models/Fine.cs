using System.ComponentModel.DataAnnotations;

namespace LibraryApp.Models
{
    public class Fine
    {
        [Key]
        public int Id_Fine { get; set; }
        public double Amount { get; set; }
        public int Id_Loan { get; set; }
    }
}
