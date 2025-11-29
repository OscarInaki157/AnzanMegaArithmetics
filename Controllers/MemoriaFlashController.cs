using AnzanMegaArithmetics.Models;
using AnzanMegaArithmetics.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AnzanMegaArithmetics.Controllers
{
    [Authorize]
    public class MemoriaFlashController : Controller
    {
        private readonly IPruebasDBService _pruebasDBService;

        public MemoriaFlashController(IPruebasDBService pruebasDBService)
        {
            this._pruebasDBService = pruebasDBService;
        }

        [HttpGet]
        public IActionResult LimpiarYDashboard()
        {
            HttpContext.Session.Remove("ConfigMemoriaFlash");

            return RedirectToAction("Memorizacion","Dashboard");
        }

        [HttpGet]
        public IActionResult FormularioMemoriaFlash() 
        {
            var userId = HttpContext.Session.GetInt32("Id_Usuario");

            if (userId == null || userId == 0) 
            {
                return RedirectToAction("Inicio", "Inicio");
            }
            
            var configStr = HttpContext.Session.GetString("ConfigMemoriaFlash");

            ConfMemoriaFlashModel config;

            if (!string.IsNullOrEmpty(configStr))
            {
                try
                {
                    config = JsonSerializer.Deserialize<ConfMemoriaFlashModel>(configStr);
                }
                catch
                {
                    config = ObtenerConfiguracionPorDefecto();
                }
            }
            else 
            {
                config = ObtenerConfiguracionPorDefecto();
            }

            return View(config);
        }

        private ConfMemoriaFlashModel ObtenerConfiguracionPorDefecto() 
        {
            return new ConfMemoriaFlashModel
            {
                CantidadEjercicios = 5,
                VelocidadPreguntas = "1",
                CategoriaEjercicios = "Lista básica",
                TipoPreguntas = "Números",
                DigitosEjercicios = 1,
                MostrarParejas = "No",
                TiempoMeditacion = 3,
                ActivarSonido = true,
                ColorA = "color1",
                ColorB = "color2"
            };
        }

        [HttpPost]
        public IActionResult ConcentracionMemoriaFlash(ConfMemoriaFlashModel config) 
        {
            //datos concentracion
            ViewBag.TiempoMeditacion = config.TiempoMeditacion;
            ViewBag.VelocidadPreguntas = config.VelocidadPreguntas;
            return View();
        }

        private readonly Dictionary<int, string> FigurasPorDigito = new Dictionary<int, string> 
        {
            { 0, "/Content/Images/0.png"},
            { 1, "/Content/Images/1.png"},
            { 2, "/Content/Images/2.png"},
            { 3, "/Content/Images/3.png"},
            { 4, "/Content/Images/4.png"},
            { 5, "/Content/Images/5.png"},
            { 6, "/Content/Images/6.png"},
            { 7, "/Content/Images/7.png"},
            { 8, "/Content/Images/8.png"},
            { 9, "/Content/Images/9.png"}
        };

        private readonly Dictionary<int, string> ParejasListaBasica = new Dictionary<int, string> 
        {
            { 1, "/Content/Images/ListaBasica/1.png"},
            { 2, "/Content/Images/ListaBasica/2.png"},
            { 3, "/Content/Images/ListaBasica/3.png"},
            { 4, "/Content/Images/ListaBasica/4.png"},
            { 5, "/Content/Images/ListaBasica/5.png"},
            { 6, "/Content/Images/ListaBasica/6.png"},
            { 7, "/Content/Images/ListaBasica/7.png"},
            { 8, "/Content/Images/ListaBasica/8.png"},
            { 9, "/Content/Images/ListaBasica/9.png"},
            { 10, "/Content/Images/ListaBasica/10.png"},
            { 11, "/Content/Images/ListaBasica/11.png"},
            { 12, "/Content/Images/ListaBasica/12.png"},
            { 13, "/Content/Images/ListaBasica/13.png"},
            { 14, "/Content/Images/ListaBasica/14.png"},
            { 15, "/Content/Images/ListaBasica/15.png"},
            { 16, "/Content/Images/ListaBasica/16.png"},
            { 17, "/Content/Images/ListaBasica/17.png"},
            { 18, "/Content/Images/ListaBasica/18.png"},
            { 19, "/Content/Images/ListaBasica/19.png"},
            { 20, "/Content/Images/ListaBasica/20.png"}
        };

        private readonly Dictionary<int, string> ParejasViajeAmerica = new Dictionary<int, string>
        {
            { 1, "/Content/Images/ViajeAmerica/1.png"},
            { 2, "/Content/Images/ViajeAmerica/2.png"},
            { 3, "/Content/Images/ViajeAmerica/3.png"},
            { 4, "/Content/Images/ViajeAmerica/4.png"},
            { 5, "/Content/Images/ViajeAmerica/5.png"},
            { 6, "/Content/Images/ViajeAmerica/6.png"},
            { 7, "/Content/Images/ViajeAmerica/7.png"},
            { 8, "/Content/Images/ViajeAmerica/8.png"},
            { 9, "/Content/Images/ViajeAmerica/9.png"},
            { 10, "/Content/Images/ViajeAmerica/10.png"},
            { 11, "/Content/Images/ViajeAmerica/11.png"},
            { 12, "/Content/Images/ViajeAmerica/12.png"},
            { 13, "/Content/Images/ViajeAmerica/13.png"},
            { 14, "/Content/Images/ViajeAmerica/14.png"},
            { 15, "/Content/Images/ViajeAmerica/15.png"},
            { 16, "/Content/Images/ViajeAmerica/16.png"},
            { 17, "/Content/Images/ViajeAmerica/17.png"},
            { 18, "/Content/Images/ViajeAmerica/18.png"},
            { 19, "/Content/Images/ViajeAmerica/19.png"},
            { 20, "/Content/Images/ViajeAmerica/20.png"}
        };

        [HttpGet]
        public IActionResult EjercicioMemoriaFlash() 
        {
            return View();
        }


    }
}
