using AnzanMegaArithmetics.Models;
using Microsoft.AspNetCore.Mvc;

namespace AnzanMegaArithmetics.Controllers
{
    public class SorobanController : Controller
    {
        public IActionResult LecturaSoroban(int ? cantidad, int ? valMin, int ? valMax, string velocidad)
        {
            var model = new ConfLecturaSorobanModel 
            {
                CantidadEjercicios = cantidad ?? 5,
                VMinimo = valMin ?? 0,
                VMaximo = valMax ?? 99,
                VelocidadPreguntas = velocidad ?? "0",
                TiempoMeditacion = 3
            }; 
            return View(model);
        }

        [HttpPost]
        public IActionResult ConcentracionLS(ConfLecturaSorobanModel modelo) 
        {
            return View();
        }
    }
}
