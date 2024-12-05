using HasheoProyecto.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DreamTeamEncript.Controllers
{
    [Authorize]
    public class HaseadorController : Controller
    {

        // Acción para mostrar la vista inicial

        public IActionResult Index()
        {
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
            // Validar si el usuario está autenticado
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            return View();
        }

        // Acción para encriptar (Hashear)
        [HttpPost]
        public IActionResult Hasear(string palabra, string key)
        {
            if (string.IsNullOrWhiteSpace(palabra) || string.IsNullOrWhiteSpace(key))
            {
                ViewData["ErrorMessage"] = "La palabra y la llave no pueden estar vacías.";
                return View("Index");
            }

            try
            {
                // Encriptar utilizando el helper
                string encrypted = CustomEncryptionStrings.Encrypt(palabra, key);
                ViewData["HashedWord"] = encrypted;
                ViewData["OriginalWord"] = palabra; // Pasar la palabra original a la vista
                ViewData["KeyUsed"] = key; // Pasar la llave utilizada
            }
            catch (Exception ex)
            {
                // Capturar y mostrar el error si falla el hasheo
                ViewData["ErrorMessage"] = $"Error al encriptar: {ex.Message}";
            }

            return View("Index");
        }

        // Acción para desencriptar (Deshashear)
        [HttpPost]
        public IActionResult Deshashear(string encryptedInput, string key)
        {
            if (string.IsNullOrWhiteSpace(encryptedInput) || string.IsNullOrWhiteSpace(key))
            {
                ViewData["ErrorMessage"] = "El texto encriptado y la llave no pueden estar vacíos.";
                return View("Index");
            }

            try
            {
                // Desencriptar utilizando el helper
                string decrypted = CustomEncryptionStrings.Decrypt(encryptedInput, key);
                ViewData["DecryptedWord"] = decrypted;
            }
            catch (Exception ex)
            {
                // Capturar y mostrar el error si falla el deshasheo
                ViewData["ErrorMessage"] = $"Error al desencriptar: {ex.Message}";
            }

            return View("Index");
        }
    }
}
