using System.ComponentModel.DataAnnotations.Schema;

namespace Library7.Models
{
	public class LoginModel
	{
		public string Email { get; set; }
		public string Password { get; set; }
		//
		[NotMapped]
		public bool KeepLogged { get; set; }
	}
}
