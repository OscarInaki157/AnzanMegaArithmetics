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
         
            ViewBag.IdUsuario = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            ViewBag.Nombre = User.FindFirst(ClaimTypes.Name)?.Value;
            ViewBag.Rol = User.FindFirst(ClaimTypes.Role)?.Value;
            ViewBag.Clase = User.FindFirst("Clase")?.Value;
            ViewBag.Usuario = User.FindFirst("Usuario")?.Value;

            
            if (string.IsNullOrEmpty(ViewBag.IdUsuario))
            {
                return RedirectToAction("Inicio", "Inicio");
            }

            return View();
        }
    }
}
