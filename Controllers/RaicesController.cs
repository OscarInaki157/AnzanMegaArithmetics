using AnzanMegaArithmetics.Models;
using AnzanMegaArithmetics.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AnzanMegaArithmetics.Controllers
{
    [Authorize]
    public class RaicesController : Controller
    {
        private readonly IPruebasDBService _pruebasDBService;
        public RaicesController(IPruebasDBService pruebasDBService)
        {
            this._pruebasDBService = pruebasDBService;
        }

        [HttpGet]
        public IActionResult LimpiarYDashboard() 
        {
            return RedirectToAction("Desafios", "Dashboard");
        }

        [HttpGet]
        public IActionResult FormularioRaices() 
        {
            var userId = HttpContext.Session.GetInt32("Id_Usuario");

            if (userId == null || userId == 0)
            {
                return RedirectToAction("Inicio", "Inicio");
            }

            ConfRaicesModel config;
            var configJson = HttpContext.Session.GetString("RaicesConf");

            if (string.IsNullOrEmpty(configJson))
            {

                config = new ConfRaicesModel
                {
                    CantidadEjercicios = 10,
                    TipoEjercicio = "2digitos",
                    TiempoTotal = 5,
                    TiempoMeditacion = 3,
                    TipoPrueba = "Práctica"
                };

                HttpContext.Session.SetString("RaicesConf", JsonSerializer.Serialize(config));

            } 
            else
            {
                config = JsonSerializer.Deserialize<ConfRaicesModel>(configJson);
                config.TipoPrueba = "Práctica";
            }
            
            return View(config);
        }

        [HttpPost]
        public IActionResult ConcentracionRaices(ConfRaicesModel config)
        {
            try
            {
                //generar ejercicios
                List<EjercicioRaicesModel> ejercicios = GenerarEjerciciosRaices(config);

                //generar respuestas
                List<RespuestaRaicesModel> respuestas = ejercicios.Select(e => new RespuestaRaicesModel
                {
                    Id_Ejercicio = e.Id_Ejercicio,
                    Respuesta_Usuario = 0,
                    Es_Correcto = false,
                    Tiempo_Respuesta = 0
                }).ToList();

                //guardar en session
                HttpContext.Session.SetString("RaicesConf", JsonSerializer.Serialize(config));
                HttpContext.Session.SetString("RaicesEjercicios", JsonSerializer.Serialize(ejercicios));
                HttpContext.Session.SetString("RaicesRespuestas", JsonSerializer.Serialize(respuestas));
                HttpContext.Session.SetInt32("EjercicioActualRaices", 1);

                HttpContext.Session.SetInt32("RaicesTiempoRestante", config.TiempoTotal * 60);
                HttpContext.Session.SetString("RaicesInicioTiempo", DateTime.Now.ToString());

                ViewBag.TiempoMeditacion = config.TiempoMeditacion;
                return View();
            }
            catch (Exception ex)
            {
                return RedirectToAction("FormularioRaices");
            }
        }

        private List<EjercicioRaicesModel> GenerarEjerciciosRaices(ConfRaicesModel config) 
        {
            List<EjercicioRaicesModel> ejercicios = new();
            Random rand = new Random();

            for (int i=0; i<config.CantidadEjercicios; i++) 
            {
                EjercicioRaicesModel ejercicio = new EjercicioRaicesModel 
                {
                    Id_Ejercicio = i + 1
                };

                if (config.TipoEjercicio.Contains("2"))
                {
                    int raiz = rand.Next(10, 99);
                    ejercicio.Numero_Base = raiz * raiz;
                }
                else if (config.TipoEjercicio.Contains("3")) 
                {
                    int raiz = rand.Next(100, 999);
                    ejercicio.Numero_Base = raiz * raiz;
                }

                ejercicios.Add(ejercicio);

            }

            return ejercicios;
        }

        [HttpGet]
        public IActionResult EjercicioRaices()
        {
            try
            {
                var configJson = HttpContext.Session.GetString("RaicesConf");
                var ejerciciosJson = HttpContext.Session.GetString("RaicesEjercicios");

                if (string.IsNullOrEmpty(ejerciciosJson) || string.IsNullOrEmpty(configJson))
                {
                    return RedirectToAction("FormularioRaices");
                }

                List<EjercicioRaicesModel> ejercicios = JsonSerializer.Deserialize<List<EjercicioRaicesModel>>(ejerciciosJson);
                ConfRaicesModel config = JsonSerializer.Deserialize<ConfRaicesModel>(configJson);
                int ejercicioActual = HttpContext.Session.GetInt32("EjercicioActualRaices") ?? 1;

                if (ejercicioActual > ejercicios.Count)
                {
                    return RedirectToAction("ResultadosRaices");
                }

                int tiempoRestante = HttpContext.Session.GetInt32("RaicesTiempoRestante") ?? 0;

                if (tiempoRestante <= 0)
                {
                    return RedirectToAction("ResultadosRaices");
                }

                EjercicioRaicesModel ejercicio = ejercicios.FirstOrDefault(e => e.Id_Ejercicio == ejercicioActual);
                if (ejercicio == null) 
                {
                    return RedirectToAction("FormularioRaices");
                }

                ViewBag.EjercicioActual = ejercicioActual;
                ViewBag.TotalEjercicios = ejercicios.Count();
                ViewBag.TiempoRestante = tiempoRestante;
                return View(ejercicio);
            }
            catch (Exception ex)
            {
                return RedirectToAction("FormularioRaices");
            }
        }


        [HttpPost]
        public IActionResult EjercicioRaices(double respuestaUsuario, double tiempoRespuesta)
        {
            try
            {
                var ejerciciosJson = HttpContext.Session.GetString("RaicesEjercicios");
                var respuestasJson = HttpContext.Session.GetString("RaicesRespuestas");
                var ejercicioActual = HttpContext.Session.GetInt32("EjercicioActualRaices") ?? 1;
                var tiempoRestante = HttpContext.Session.GetInt32("RaicesTiempoRestante") ?? 0;

                if (string.IsNullOrEmpty(ejerciciosJson) || string.IsNullOrEmpty(respuestasJson))
                {
                    return RedirectToAction("FormularioRaices");
                }

                List<EjercicioRaicesModel> ejercicios = JsonSerializer.Deserialize<List<EjercicioRaicesModel>>(ejerciciosJson);
                List<RespuestaRaicesModel> respuestas = JsonSerializer.Deserialize<List<RespuestaRaicesModel>>(respuestasJson);

                var ejercicio = ejercicios.FirstOrDefault(e => e.Id_Ejercicio == ejercicioActual);
                var respuesta = respuestas.FirstOrDefault(r => r.Id_Ejercicio == ejercicioActual);

                if (ejercicio == null || respuesta == null)
                {
                    return RedirectToAction("FormularioRaices");
                }

                respuesta.Respuesta_Usuario = respuestaUsuario;
                respuesta.Es_Correcto = Math.Abs(Math.Sqrt(ejercicio.Numero_Base) - respuestaUsuario) < 0.0001;
                respuesta.Tiempo_Respuesta = tiempoRespuesta;

                HttpContext.Session.SetString("RaicesRespuestas", JsonSerializer.Serialize(respuestas));

                if(tiempoRestante > 0) 
                {
                    tiempoRestante = Math.Max(0, tiempoRestante - (int)Math.Ceiling(tiempoRespuesta));
                }

                HttpContext.Session.SetInt32("RaicesTiempoRestante", tiempoRestante);
                HttpContext.Session.SetInt32("EjercicioActualRaices", ejercicioActual + 1);

                if (ejercicioActual + 1 > ejercicios.Count() || tiempoRestante <= 0)
                {
                    return RedirectToAction("ResultadosRaices");
                }
                else 
                {
                    return RedirectToAction("EjercicioRaices");
                }

            }
            catch (Exception ex) 
            {
                return RedirectToAction("FormularioRaices");
            }
        }

        [HttpGet]
        public IActionResult ResultadosRaices()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("Id_Usuario");
                if (userId == null || userId == 0)
                {
                    return RedirectToAction("Inicio", "Inicio");
                }

                //obtener datos de session
                var ejerciciosJson = HttpContext.Session.GetString("RaicesEjercicios");
                var respuestasJson = HttpContext.Session.GetString("RaicesRespuestas");
                var configJson = HttpContext.Session.GetString("RaicesConf");

                if (string.IsNullOrEmpty(ejerciciosJson) || string.IsNullOrEmpty(respuestasJson) || string.IsNullOrEmpty(configJson))
                {
                    return RedirectToAction("FormularioRaices");
                }

                var ejercicios = JsonSerializer.Deserialize<List<EjercicioRaicesModel>>(ejerciciosJson);
                var respuestas = JsonSerializer.Deserialize<List<RespuestaRaicesModel>>(respuestasJson);
                var config = JsonSerializer.Deserialize<ConfRaicesModel>(configJson);

                int correctas = respuestas.Count(r => r.Es_Correcto);
                int incorrectas = respuestas.Count(r => !r.Es_Correcto);
                int sinResponder = ejercicios.Count - respuestas.Count(r => r.Respuesta_Usuario != 0);

                double porcentajeAcierto= ejercicios.Count > 0? (correctas * 100.0) / ejercicios.Count : 0;
                double tiempoPromedio = respuestas.Where(r => r.Tiempo_Respuesta > 0).DefaultIfEmpty().Average(r => r?.Tiempo_Respuesta ?? 0);
                double tiempoTotal = respuestas.Sum(r => r.Tiempo_Respuesta);

                var modeloResultados = new ResRaicesViewModel
                {
                    Ejercicios = ejercicios,
                    Respuestas = respuestas,
                    Configuracion = config,
                    TotalCorrectas = correctas,
                    TotalIncorrectas = incorrectas,
                    TotalSinResponder = sinResponder,
                    PorcentajeAcierto = porcentajeAcierto,
                    TiempoPromedio = tiempoPromedio,
                    TiempoTotal = tiempoTotal,
                    FechaPrueba = DateTime.Now
                };

                HttpContext.Session.Remove("RaicesEjercicios");
                HttpContext.Session.Remove("RaicesRespuestas");
                HttpContext.Session.Remove("EjercicioActualRaices");
                //HttpContext.Session.Remove("RaicesConf");
                HttpContext.Session.Remove("RaicesTiempoRestante");


                return View(modeloResultados);
            }
            catch (Exception ex)
            {
                return RedirectToAction("FormularioRaices");
            }
        }


    }
}
