using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryApp.Models
{
    public class Loan
    {
        [Key]
        public int Id_Loan { get; set; }

        [DataType(DataType.Date)]
        [Column(TypeName = "Date")]
        public DateTime LoanDate { get; set; }

        [DataType(DataType.Date)]
        [Column(TypeName = "Date")]
        public DateTime DueDate { get; set; }

        public DateTime? ReturnDate { get; set; }

        public int Id_Book { get; set; }

        public int Id_Member { get; set; }

        public bool Finished { get; set; }

        public double? Fine { get; set; }
    }
}
