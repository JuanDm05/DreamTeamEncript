using Microsoft.AspNetCore.Mvc;
using System.Data;
using Dapper;
using Npgsql;
using HasheoProyecto.Helpers;
using DreamTeamEncript.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization; // Asegúrate de que el namespace de la clase CustomEncryption coincida

namespace DreamTeamEncript.Controllers
{
    public class AccountController : Controller
    {
        private readonly IDbConnection _dbConnection;
        public AccountController(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        //public IActionResult Index()
        //{

        //    return View();

        //}

        public IActionResult Register()
        {
            return View();
        }
        [AllowAnonymous]

        public IActionResult Login(string returnUrl = null)
        {
            // Verificar y limpiar ciclos de ReturnUrl
            if (!string.IsNullOrEmpty(returnUrl) &&
                Url.IsLocalUrl(returnUrl) &&
                !returnUrl.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase))
            {
                ViewData["ReturnUrl"] = returnUrl;
            }
            else
            {
                ViewData["ReturnUrl"] = Url.Action("Index", "Home");
            }

            return View();
        }


        // Registro de usuario
        [HttpPost]
        public async Task<IActionResult> Register(string username, string password, string email)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError("", "Todos los campos son obligatorios.");
                return View();
            }

            // Verificar duplicados
            string checkQuery = "SELECT COUNT(*) FROM Usuarios1 WHERE Username = @Username OR Email = @Email";
            int count = await _dbConnection.ExecuteScalarAsync<int>(checkQuery, new { Username = username, Email = email });

            if (count > 0)
            {
                ModelState.AddModelError("", "El nombre de usuario o correo electrónico ya está registrado.");
                return View();
            }

            // Encriptar contraseña
            string userKey = username;
            string encryptedPassword = CustomEncryption.Encrypt(password, userKey);

            // Insertar usuario
            string query = "INSERT INTO Usuarios1 (Username, Password, Email) VALUES (@Username, @Password, @Email)";
            await _dbConnection.ExecuteAsync(query, new { Username = username, Password = encryptedPassword, Email = email });

            ViewBag.Message = "Usuario registrado exitosamente.";
            return RedirectToAction("Login");
        }


        // Login de usuario

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewData["ErrorMessage"] = "Todos los campos son obligatorios.";
                return View();
            }

            string query = "SELECT Username, Password FROM Usuarios1 WHERE Username = @Username";
            var user = await _dbConnection.QuerySingleOrDefaultAsync<Usuarios>(query, new { Username = username });

            if (user == null || string.IsNullOrWhiteSpace(user.password))
            {
                ViewData["ErrorMessage"] = "Usuario o contraseña incorrectos.";
                return View();
            }

            try
            {
                string userKey = username;
                string decryptedPassword = CustomEncryption.Decrypt(user.password, userKey);

                if (decryptedPassword != password)
                {
                    ViewData["ErrorMessage"] = "Contraseña incorrecta.";
                    return View();
                }
                // Crear la cookie de autenticación
                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username) // Nombre de usuario
        };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true, // La cookie será persistente
                    ExpiresUtc = DateTime.UtcNow.AddSeconds(120) // Expiración de la cookie
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                                              new ClaimsPrincipal(claimsIdentity),
                                              authProperties);

                return RedirectToAction("Index", "Haseador");
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] = $"Error al verificar la contraseña: {ex.Message}";
                return View();
            }
        }
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
    }
}