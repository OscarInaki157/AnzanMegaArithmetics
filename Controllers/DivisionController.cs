using AnzanMegaArithmetics.Models;
using AnzanMegaArithmetics.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnzanMegaArithmetics.Controllers
{
    [Authorize]
    public class DivisionController : Controller
    {
        private readonly IPruebasDBService _pruebasDBService;
        public DivisionController(IPruebasDBService pruebasDBService)
        {
            _pruebasDBService = pruebasDBService;
        }

        //Division normal
        [HttpGet]
        public IActionResult LimpiarDivisionYDashboard()
        {
            HttpContext.Session.Remove("ConfDivision");
            HttpContext.Session.Remove("EjercicioActual");
            HttpContext.Session.Remove("ResDivision");
            HttpContext.Session.Remove("UltimaConfigDivision");
            HttpContext.Session.Remove("InicioEjercicio");

            return RedirectToAction("Dashboard", "Dashboard");
        }

        [HttpPost]
        public IActionResult RepetirDivision()
        {
            var configJson = HttpContext.Session.GetString("UltimaConfigDivision");

            if (!string.IsNullOrEmpty(configJson))
            {
                var config = System.Text.Json.JsonSerializer.Deserialize<ConfDivModel>(configJson);

                HttpContext.Session.SetString("ConfDivision", configJson);
                HttpContext.Session.SetInt32("EjercicioActual", 1);
                HttpContext.Session.SetString("ResDivision", System.Text.Json.JsonSerializer.Serialize(new List<RDivisionModel>()));

                ViewBag.TiempoMeditacion = config.TiempoMeditacion;
                ViewBag.VelocidadPreguntas = float.Parse(config.VelocidadPreguntas.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);

                return View("ConcentracionDivision");
            }

            return RedirectToAction("DivisionForm");
        }

        [HttpGet]
        public IActionResult DivisionForm()
        {
            var userId = HttpContext.Session.GetInt32("Id_Usuario");

            if (userId == null || userId == 0)
            {
                return RedirectToAction("Inicio", "Inicio");
            }

            var configJson = HttpContext.Session.GetString("UltimaConfigDivision");
            ConfDivModel modelo;

            if (!string.IsNullOrEmpty(configJson))
            {
                modelo = System.Text.Json.JsonSerializer.Deserialize<ConfDivModel>(configJson);
            }
            else
            {
                modelo = new ConfDivModel
                {
                    CantidadEjercicios = 5,
                    FormatoPregunta = "galera",
                    DireccionRespuesta = "DerechaAIzquierda",
                    VelocidadPreguntas = "0.0",
                    DigitosDividendo = "2",
                    DigitosDivisor = "2",
                    TiempoMeditacion = 3,
                    TipoEjercicio = "exacta"
                };
            }

            return View(modelo);
        }

        [HttpPost]
        public IActionResult ConcentracionDivision(ConfDivModel config) 
        {
            HttpContext.Session.SetString("ConfDivision", System.Text.Json.JsonSerializer.Serialize(config));
            HttpContext.Session.SetInt32("EjercicioActual", 1);
            HttpContext.Session.SetString("ResDivision", System.Text.Json.JsonSerializer.Serialize(new List<RDivisionModel>()));
            HttpContext.Session.SetString("UltimaConfigDivision", System.Text.Json.JsonSerializer.Serialize(config));

            ViewBag.TiempoMeditacion = config.TiempoMeditacion;
            ViewBag.VelocidadPreguntas = float.Parse(config.VelocidadPreguntas.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);

            return View();
        }

        [HttpGet]
        public IActionResult EjercicioDivision() 
        {
            var configJson = HttpContext.Session.GetString("ConfDivision");
            if (string.IsNullOrEmpty(configJson)) return RedirectToAction("DivisionForm");

            var config = System.Text.Json.JsonSerializer.Deserialize<ConfDivModel>(configJson);
            var ejercicioActual = HttpContext.Session.GetInt32("EjercicioActual") ?? 1;

            if (ejercicioActual > config.CantidadEjercicios)
            {
                return RedirectToAction("ResultadosDivision");
            }

            var listaJson = HttpContext.Session.GetString("ResDivision");
            List<RDivisionModel> listaEjercicios;

            if (string.IsNullOrEmpty(listaJson) ||
                System.Text.Json.JsonSerializer.Deserialize<List<RDivisionModel>>(listaJson).Count == 0)
            {
                listaEjercicios = GenerarEjercicios(config);
                HttpContext.Session.SetString("ResDivision", System.Text.Json.JsonSerializer.Serialize(listaEjercicios));
            }
            else
            {
                listaEjercicios = System.Text.Json.JsonSerializer.Deserialize<List<RDivisionModel>>(listaJson);
            }

            var modeloEjercicio = listaEjercicios[ejercicioActual - 1];

            ViewBag.Config = config;
            ViewBag.NumEjercicio = ejercicioActual;

            return View(modeloEjercicio);
        }

        private List<RDivisionModel> GenerarEjercicios(ConfDivModel config)
        {
            var lista = new List<RDivisionModel>();
            var random = new Random();

            int digitosDividendo = int.Parse(config.DigitosDividendo);
            int digitosDivisor = int.Parse(config.DigitosDivisor);

            int minDiv = (int)Math.Pow(10, digitosDividendo - 1);
            int maxDiv = (int)Math.Pow(10, digitosDividendo) - 1;

            int minDvr = (int)Math.Pow(10, digitosDivisor - 1);
            int maxDvr = (int)Math.Pow(10, digitosDivisor) - 1;

            for (int i = 0; i < config.CantidadEjercicios; i++)
            {
                int dividendo = random.Next(minDiv, maxDiv + 1);
                int divisor = random.Next(minDvr, maxDvr + 1);

                if (config.TipoEjercicio == "exacta")
                {
                    int cociente = dividendo / divisor;
    
                    if (cociente == 0) cociente = 1;

                    int nuevoDividendo = divisor * cociente;

                    if (nuevoDividendo < minDiv) nuevoDividendo += divisor;

                    if (nuevoDividendo > maxDiv) nuevoDividendo -= divisor;

                    dividendo = nuevoDividendo;
                }

                lista.Add(new RDivisionModel
                {
                    Dividendo = dividendo,
                    Divisor = divisor,
                    RespuestaUsuario = -1
                });
            }
            return lista;
        }

        [HttpPost]
        public IActionResult EnviarRespuesta(double respuestaUsuario, double tiempoRespuesta)
        {
            var listaJson = HttpContext.Session.GetString("ResDivision");
            var ejercicioActual = HttpContext.Session.GetInt32("EjercicioActual") ?? 1;
            var configJson = HttpContext.Session.GetString("ConfDivision");

            if (string.IsNullOrEmpty(listaJson) || string.IsNullOrEmpty(configJson))
                return RedirectToAction("DivisionForm");

            var lista = System.Text.Json.JsonSerializer.Deserialize<List<RDivisionModel>>(listaJson);
            var config = System.Text.Json.JsonSerializer.Deserialize<ConfDivModel>(configJson);

            if (ejercicioActual <= lista.Count)
            {
                var item = lista[ejercicioActual - 1];
                item.RespuestaUsuario = respuestaUsuario;
                item.TiempoRespuesta = tiempoRespuesta;
            }

            HttpContext.Session.SetString("ResDivision", System.Text.Json.JsonSerializer.Serialize(lista));

            if (ejercicioActual < config.CantidadEjercicios)
            {
                HttpContext.Session.SetInt32("EjercicioActual", ejercicioActual + 1);
                return RedirectToAction("EjercicioDivision");
            }
            else
            {
                return RedirectToAction("ResultadosDivision");
            }
        }


        [HttpPost]
        public IActionResult FinalizarDivision(double? respuestaUsuario, double tiempoRespuesta)
        {
            var configJson = HttpContext.Session.GetString("ConfDivision");
            var resJson = HttpContext.Session.GetString("ResDivision");

            if (string.IsNullOrEmpty(configJson))
                return RedirectToAction("DivisionForm");

            var config = System.Text.Json.JsonSerializer.Deserialize<ConfDivModel>(configJson);
            var ejercicioActual = HttpContext.Session.GetInt32("EjercicioActual") ?? 1;

            List<RDivisionModel> resultados;
            if (!string.IsNullOrEmpty(resJson))
            {
                resultados = System.Text.Json.JsonSerializer.Deserialize<List<RDivisionModel>>(resJson);
            }
            else
            {
                resultados = GenerarEjercicios(config);
            }

            if (respuestaUsuario.HasValue && respuestaUsuario != -1 && ejercicioActual <= resultados.Count)
            {
                var item = resultados[ejercicioActual - 1];
                item.RespuestaUsuario = respuestaUsuario.Value;
                item.TiempoRespuesta = tiempoRespuesta;
            }

            HttpContext.Session.SetString("ResDivision", System.Text.Json.JsonSerializer.Serialize(resultados));

            return RedirectToAction("ResultadosDivision");
        }

        [HttpGet]
        public IActionResult ResultadosDivision()
        {
            var userId = HttpContext.Session.GetInt32("Id_Usuario");

            if (userId == null || userId == 0)
            {
                return RedirectToAction("Inicio", "Inicio");
            }

            var resJson = HttpContext.Session.GetString("ResDivision");

            if (!string.IsNullOrEmpty(resJson))
            {
                TempData["Resultados"] = resJson;
            }

            var resultados = string.IsNullOrEmpty(resJson)
                ? new List<RDivisionModel>()
                : System.Text.Json.JsonSerializer.Deserialize<List<RDivisionModel>>(resJson);

            int total = resultados.Count;
            int correctas = resultados.Count(r => r.EsCorrecto);
            int incorrectas = resultados.Count(r => r.Respondido && !r.EsCorrecto);

            int porcentaje = total > 0 ? (correctas * 100) / total : 0;
            int xp = porcentaje;


            double tiempoTotalSegundos = resultados.Sum(r => r.TiempoRespuesta);
            TimeSpan tiempoTotal = TimeSpan.FromSeconds(tiempoTotalSegundos);

            PruebasDBModel results = new PruebasDBModel
            {
                Id_Usuario = userId.Value,
                Total_Preguntas = total,
                Respuestas_Correctas = correctas,
                Fecha = DateTime.Now,
                Tipo_Prueba = "División",
                ExperienciaAdquirida = xp,
                Tiempo = tiempoTotal
            };


            bool insertarPrueba = _pruebasDBService.GuardarPrueba(results);

            ViewBag.Total = total;
            ViewBag.Correctas = correctas;
            ViewBag.Incorrectas = incorrectas;
            ViewBag.Porcentaje = porcentaje;
            ViewBag.TiempoTotal = tiempoTotal;

            return View(resultados);
        }
    }
}
