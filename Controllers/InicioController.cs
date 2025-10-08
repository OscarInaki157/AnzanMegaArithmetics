using AnzanMegaArithmetics.Models;
using AnzanMegaArithmetics.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AnzanMegaArithmetics.Controllers
{
    public class InicioController : Controller
    {
        private readonly IUsersDBService _usersDBService;
        public InicioController(IUsersDBService usersDBService) 
        {
            this._usersDBService = usersDBService;
        }
        
        public IActionResult Inicio()
        {
            return View();
        }

        public IActionResult Nosotros() 
        {
            return View();
        }

        [HttpGet]
        public IActionResult Login() 
        {
            return View();
        }

        public IActionResult Registro() 
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ValidarUser(LoginRequestModel model)
        {
            string user = model.Usuario;
            string pass = model.Pass;

            LoginResponseModel response = _usersDBService.ValidateLogin(user, pass);

            if (response.Gamer_Tag.Contains("Error al validar") || response.Gamer_Tag.Contains("No hay coincidencias") || 
                response.Licencia.Contains("Vencida") || response.Licencia.Contains("Sin Licencia"))
            {

                string mensajeError = response.Gamer_Tag.Contains("Error al validar") || response.Gamer_Tag.Contains("No hay coincidencias")
           ? "Error al validar, usuario no encontrado."
           : "Tu licencia no está activa. Contacta al administrador.";

                ViewBag.ErrorMessage = mensajeError;

                return View("Login");
            }

            //actualizar ultima conexion del chabon

            bool cnx = _usersDBService.UltimaConexion(response);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, response.Id_Usuario.ToString()),
                new Claim(ClaimTypes.Name, response.Nombre),
                new Claim(ClaimTypes.Role, response.Id_Rol),
                new Claim("Usuario", response.Gamer_Tag),
                new Claim("Correo", response.Correo),
                new Claim("Racha", response.Racha.ToString()),
                new Claim("Exp", response.Exp.ToString()),
                new Claim("UltimaCnx", response.Ultima_Cnx.ToString("yyyy-MM-dd HH:mm:ss")),
                new Claim("Licencia", response.Licencia)
            };

            claims.Add(new Claim("Clases", string.Join(",", response.Clases)));

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            // Crear un principal de claims
            var authProperties = new AuthenticationProperties
            {
                AllowRefresh = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30),
            };

            // Firmar al usuario
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            return RedirectToAction("Dashboard", "Dashboard");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Inicio", "Inicio");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}
