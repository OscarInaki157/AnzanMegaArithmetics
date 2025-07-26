using AnzanMegaArithmetics.Models;
using Microsoft.AspNetCore.Mvc;

namespace AnzanMegaArithmetics.Controllers
{
    public class ConferenciasController : Controller
    {
        public IActionResult PanelFormFinger()
        {
            return View();
        }

        [HttpPost]
        public IActionResult FingerPanel(ConfConferencias config)
        {
            HttpContext.Session.SetInt32("ColorBrillo", config.Color);
            return View();
        }

        public IActionResult PanelFormSoroban() 
        {
            return View();
        }

        [HttpPost]
        public IActionResult SorobanPanel(ConfConferencias config)
        {
            HttpContext.Session.SetInt32("ColorBrillo", config.Color);
            return View();
        }

    }
}
