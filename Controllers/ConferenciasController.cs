using AnzanMegaArithmetics.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnzanMegaArithmetics.Controllers
{
    [Authorize]
    public class ConferenciasController : Controller
    {
        public IActionResult PanelFormFinger()
        {
            return View();
        }

        [HttpPost]
        public IActionResult FingerPanel(ConfConferencias config)
        {
            HttpContext.Session.SetInt32("EstiloManos", config.Estilo);
            return View();
        }

        public IActionResult PanelFormSoroban() 
        {
            return View();
        }

        [HttpPost]
        public IActionResult SorobanPanel(ConfConferencias config)
        {
            HttpContext.Session.SetInt32("EstiloSoroban", config.Estilo);
            return View();
        }

    }
}
