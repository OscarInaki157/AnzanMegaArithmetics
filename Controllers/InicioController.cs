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

            if (response.Usuario.Contains("Error al validar") || response.Usuario.Contains("No hay coincidencias"))
            {
                ViewBag.ErrorMessage = response.Usuario;
                return View("Login");
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, response.Id_Usuario.ToString()),
                new Claim(ClaimTypes.Name, response.Nombre),
                new Claim(ClaimTypes.Role, response.Rol),
                new Claim("Clase", response.Clase),
                new Claim("Usuario", response.Usuario)
            };

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
