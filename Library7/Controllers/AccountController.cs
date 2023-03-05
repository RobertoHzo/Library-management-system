using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Library7.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.SqlClient;
using Library7.Data;

namespace Library7.Controllers
{
	public class AccountController : Controller
	{
		private readonly Library7Context _context;
		private readonly DBContext1 _dbContext;

		public AccountController(Library7Context context, DBContext1 dbContext)
		{
			_context = context;
			_dbContext = dbContext;
		}

		#region Views
		public ActionResult Login()
		{
			// Verifica si hay una sesion en cookies
			ClaimsPrincipal c = HttpContext.User;
			if (c.Identity != null)
			{
				if (c.Identity.IsAuthenticated)
				{
					return RedirectToAction("Index", "Home");
				}
			}
			return View();
		}
		public ActionResult Register()
		{
			// Verifica si hay una sesion en cookies
			ClaimsPrincipal c = HttpContext.User;
			if (c.Identity != null)
			{
				if (c.Identity.IsAuthenticated)
				{
					return RedirectToAction("Index", "Home");
				}
			}
			return View();
		}
		public ActionResult UserLogin()
		{
			// Verifica si hay una sesion en cookies
			ClaimsPrincipal c = HttpContext.User;
			if (c.Identity != null)
			{
				if (c.Identity.IsAuthenticated)
				{
					return RedirectToAction("Index", "Home");
				}
			}
			return View();
		}

		#endregion
		#region Actions
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> Login(LoginModel l)
		{
			try
			{
				if (ModelState.IsValid)
				{
					using (SqlConnection con = new(_dbContext.Valor))
					{
						// llama al sp
						using (SqlCommand cmd = new("sp_MemberLogin", con))
						{
							cmd.CommandType = CommandType.StoredProcedure;

							cmd.Parameters.Add("@Email", SqlDbType.VarChar).Value = l.Email;
							cmd.Parameters.Add("@Password", SqlDbType.VarChar).Value = l.Password;
							con.Open();

							SqlDataReader dr = cmd.ExecuteReader();
							// si se ejecuta correctamente el sp se realiza lo siguiente:
							if (dr.Read())
							{
								string type = "Member";
								string id_member = dr["Id_Member"].ToString();
								string name = dr["Name"].ToString() + " " + dr["Lastname"].ToString();

								var c = new[] { new Claim("Id", id_member),
								new Claim(ClaimTypes.Role, type),
								new Claim(ClaimTypes.Name, name)};

								ClaimsIdentity ci = new(c, CookieAuthenticationDefaults.AuthenticationScheme);
								AuthenticationProperties p = new();

								p.AllowRefresh = true;
								p.IsPersistent = l.KeepLogged;

								if (!l.KeepLogged)
								{
									p.ExpiresUtc = DateTime.UtcNow.AddHours(3);
								}
								else
								{
									p.ExpiresUtc = DateTime.UtcNow.AddDays(3);
								}
								await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(ci), p);

								return RedirectToAction("Index", "Home");
							}
							// si no
							else
							{
								ViewData["error"] = "Error de credenciales";
							}
						}
						con.Close();
					}
					return RedirectToAction("Index", "Home");
				}
			}
			catch (System.Exception e)
			{
				ViewBag.Error = e.Message;
				return View();
			}
			return View();
		}
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Register([Bind("Name,Lastname,Email,Password,City,Address,Zip,Phone")] Member member)
		{
			if (ModelState.IsValid)
			{
				_context.Add(member);
				await _context.SaveChangesAsync();
				return RedirectToAction("Index", "Home");
			}
			return View(member);
		}
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> UserLogin(LoginModel l)
		{
			try
			{
				if (ModelState.IsValid)
				{
					using (SqlConnection con = new(_dbContext.Valor))
					{
						// llama al sp
						using (SqlCommand cmd = new("sp_UserLogin", con))
						{
							cmd.CommandType = CommandType.StoredProcedure;

							cmd.Parameters.Add("@Email", SqlDbType.VarChar).Value = l.Email;
							cmd.Parameters.Add("@Password", SqlDbType.VarChar).Value = l.Password;
							con.Open();

							SqlDataReader dr = cmd.ExecuteReader();
							// si se ejecuta correctamente el sp se realiza lo siguiente:
							if (dr.Read())
							{
								string type = "User";
								string id_user = dr["Id_User"].ToString();
								string name = dr["Name"].ToString() + " " + dr["Lastname"].ToString();

								var c = new[] { new Claim("Id", id_user),
								new Claim(ClaimTypes.Role, type),
								new Claim(ClaimTypes.Name, name)};

								ClaimsIdentity ci = new(c, CookieAuthenticationDefaults.AuthenticationScheme);
								AuthenticationProperties p = new();

								p.AllowRefresh = true;
								p.IsPersistent = l.KeepLogged;

								if (!l.KeepLogged)
								{
									p.ExpiresUtc = DateTime.UtcNow.AddHours(3);
								}
								else
								{
									p.ExpiresUtc = DateTime.UtcNow.AddDays(3);
								}
								await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(ci), p);

								return RedirectToAction("Index", "Home");
							}
							// si no
							else
							{
								ViewData["error"] = "Error de credenciales";
							}
						}
						con.Close();
					}
					return RedirectToAction("Index", "Home");
				}
			}
			catch (System.Exception e)
			{
				ViewBag.Error = e.Message;
				return View();
			}
			return View();
		}
		#endregion

		public async Task<IActionResult> Logout()
		{
			//Response.Cookies.Delete("user");
			//return RedirectToAction("Index", "Home");
			await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
			return RedirectToAction("Login", "Account");
		}

	}
}
