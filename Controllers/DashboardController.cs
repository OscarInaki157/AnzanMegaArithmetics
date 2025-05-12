using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AnzanMegaArithmetics.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        public DashboardController() 
        {

        }
        public IActionResult Dashboard()
        {
            var idUsuario = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var nombre = User.FindFirst(ClaimTypes.Name)?.Value;
            var rol = User.FindFirst(ClaimTypes.Role)?.Value;
            var clase = User.FindFirst("Clase")?.Value;
            var usuario = User.FindFirst("Usuario")?.Value;

            ViewBag.IdUsuario = idUsuario;
            ViewBag.Nombre = nombre;
            ViewBag.Rol = rol;
            ViewBag.Clase = clase;
            ViewBag.Usuario = usuario;

            var frases = new List<string>
            {
                $"¡Hola {nombre}, bienvenido a Mentes México!",
                $"¡{nombre}, hoy es un gran día para aprender!",
                $"¡Tu mente es poderosa, {nombre}!",
                $"¡Listo para un nuevo desafío, {nombre}!",
                $"¡Vamos a hacer magia con los números, {nombre}!",
                $"¡Cada clic te acerca a la maestría, {nombre}!"
            };

            var random = new Random();
            int index = random.Next(frases.Count);
            ViewBag.FraseBienvenida = frases[index];

            if (string.IsNullOrEmpty(idUsuario))
            {
                return RedirectToAction("Inicio", "Inicio");
            }

            return View();
        }

    }
}
