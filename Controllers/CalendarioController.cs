using Microsoft.AspNetCore.Mvc;
using AnzanMegaArithmetics.Services;
using Microsoft.AspNetCore.Authorization;
using AnzanMegaArithmetics.Models;
using System.Text.Json;
using System.Globalization;

namespace AnzanMegaArithmetics.Controllers
{
    [Authorize]
    public class CalendarioController : Controller
    {
        private readonly IPruebasDBService _pruebasDBService;
        public CalendarioController(IPruebasDBService pruebasDBService)
        {
            this._pruebasDBService = pruebasDBService;
        }

        [HttpGet]
        public IActionResult FormularioCalendario()
        {
            var userId = HttpContext.Session.GetInt32("Id_Usuario");

            if (userId == null || userId == 0)
            {
                return RedirectToAction("Inicio", "Inicio");
            }

            CalendarioConfModel config;
            var configJson = HttpContext.Session.GetString("CalendarioConfig");

            if (string.IsNullOrEmpty(configJson))
            {

                config = new CalendarioConfModel
                {
                    CantidadEjercicios = 10,
                    AnnoInicio = 2000,
                    AnnoFin = 2023,
                    TiempoLimiteMinutos = 5,
                    TiempoConcentracion = 3,
                    TipoOperacion = "anno unico",
                    TipoPrueba = "Práctica"
                };


                HttpContext.Session.SetString("CalendarioConfig", JsonSerializer.Serialize(config));
            }
            else
            {
                config = JsonSerializer.Deserialize<CalendarioConfModel>(configJson);
                config.TipoPrueba = "Práctica";
            }

            return View(config);
        }

        [HttpPost]
        public IActionResult ConcentracionCalendario(CalendarioConfModel config)
        {

            try 
            {
                //generar ejercicios
                List<EjercicioCalendarModel> ejercicios = GenerarEjerciciosCalendario(config);

                //Generar respuestas
                List<CalendarioRespuestaModel> respuestas = ejercicios.Select(e => new CalendarioRespuestaModel
                {
                    Id_Ejercicio = e.Id_Ejercicio,
                    Respuesta_Usuario = string.Empty,
                    Es_Correcto = false,
                    Tiempo_Respuesta = 0
                }).ToList();

                ViewBag.TiempoMeditacion= config.TiempoConcentracion;
                HttpContext.Session.SetString("CalendarioConfiguracion", JsonSerializer.Serialize(config));
                HttpContext.Session.SetString("CalendarioEjercicios", JsonSerializer.Serialize(ejercicios));
                HttpContext.Session.SetString("CalendarioRespuestas", JsonSerializer.Serialize(respuestas));
                HttpContext.Session.SetInt32("CalendarioEjercicioActual", 1);

                HttpContext.Session.SetInt32("CalendarioTiempoRestante", config.TiempoLimiteMinutos * 60);
                HttpContext.Session.SetString("CalendarioInicioTiempo", DateTime.Now.ToString());

                return View();
            }
            catch (Exception ex) 
            {
                return RedirectToAction("Inicio", "Inicio");
            }
        }

        [HttpGet]
        public IActionResult EjercicioCalendario()
        {
            try
            {
                var configJson = HttpContext.Session.GetString("CalendarioConfiguracion");
                var ejerciciosJson = HttpContext.Session.GetString("CalendarioEjercicios");

                if (string.IsNullOrEmpty(ejerciciosJson) || string.IsNullOrEmpty(configJson))
                {
                    return RedirectToAction("FormularioCalendario");
                }

                List<EjercicioCalendarModel> actuales = JsonSerializer.Deserialize<List<EjercicioCalendarModel>>(ejerciciosJson);
                CalendarioConfModel config = JsonSerializer.Deserialize<CalendarioConfModel>(configJson);
                int ejercicioActual = HttpContext.Session.GetInt32("CalendarioEjercicioActual") ?? 1;

                if (ejercicioActual > actuales.Count)
                {
                    //redirigir a resultados
                    return RedirectToAction("ResultadosCalendario");
                }

                int tiempoRestante = HttpContext.Session.GetInt32("CalendarioTiempoRestante") ?? 0;

                if (tiempoRestante <= 0)
                {
                    return RedirectToAction("ResultadosCalendario");
                }


                EjercicioCalendarModel ejercicio = actuales.FirstOrDefault(e => e.Id_Ejercicio == ejercicioActual);
                if (ejercicio == null) 
                {
                    return RedirectToAction("FormularioCalendario");
                }

                ViewBag.EjercicioActual = ejercicioActual;
                ViewBag.TotalEjercicios = config.CantidadEjercicios;
                ViewBag.TiempoRestante = tiempoRestante;

                return View(ejercicio);
            }
            catch (Exception ex)
            {
                return RedirectToAction("FormularioCalendario");
            }
        }

        [HttpPost]
        public IActionResult EjercicioCalendario([FromForm] string respuestaUsuario, [FromForm] double tiempoRespuesta)
        {
            try
            {
                var ejerciciosJson = HttpContext.Session.GetString("CalendarioEjercicios");
                var respuestasJson = HttpContext.Session.GetString("CalendarioRespuestas");
                var ejercicioActual = HttpContext.Session.GetInt32("CalendarioEjercicioActual") ?? 1;
                var tiempoRestante = HttpContext.Session.GetInt32("CalendarioTiempoRestante") ?? 0;

                if (string.IsNullOrEmpty(ejerciciosJson) || string.IsNullOrEmpty(respuestasJson))
                {
                    return RedirectToAction("FormularioCalendario");
                }

                List<EjercicioCalendarModel> ejercicios = JsonSerializer.Deserialize<List<EjercicioCalendarModel>>(ejerciciosJson);
                List<CalendarioRespuestaModel> respuestas = JsonSerializer.Deserialize<List<CalendarioRespuestaModel>>(respuestasJson);

                if (tiempoRestante <= 0) 
                {
                    return RedirectToAction("ResultadosCalendario");
                }

                var ejercicio = ejercicios.FirstOrDefault(e => e.Id_Ejercicio == ejercicioActual);
                var respuesta = respuestas.FirstOrDefault(r => r.Id_Ejercicio == ejercicioActual);

                if (ejercicio == null || respuesta == null)
                {
                    return RedirectToAction("FormularioCalendario");
                }

                respuesta.Respuesta_Usuario = respuestaUsuario;
                respuesta.Es_Correcto = string.Equals(respuestaUsuario.Trim(), ejercicio.Dia_Correcto, StringComparison.OrdinalIgnoreCase);
                respuesta.Tiempo_Respuesta = tiempoRespuesta;

                HttpContext.Session.SetString("CalendarioRespuestas", JsonSerializer.Serialize(respuestas));


                tiempoRestante -= (int)Math.Ceiling(tiempoRespuesta);
                if (tiempoRestante < 0) tiempoRestante = 0;

                HttpContext.Session.SetInt32("CalendarioTiempoRestante", tiempoRestante);
                HttpContext.Session.SetInt32("CalendarioEjercicioActual", ejercicioActual + 1);

                return RedirectToAction("EjercicioCalendario");
            }
            catch (Exception ex)
            {
                return RedirectToAction("FormularioCalendario");
            }
        }


        [HttpGet]
        public IActionResult ResultadosCalendario()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("Id_Usuario");
                if (userId == null || userId == 0)
                {
                    return RedirectToAction("Inicio", "Inicio");
                }

                // Obtener datos de la sesión
                var ejerciciosJson = HttpContext.Session.GetString("CalendarioEjercicios");
                var respuestasJson = HttpContext.Session.GetString("CalendarioRespuestas");
                var configJson = HttpContext.Session.GetString("CalendarioConfiguracion");

                if (string.IsNullOrEmpty(ejerciciosJson) || string.IsNullOrEmpty(respuestasJson))
                {
                    return RedirectToAction("FormularioCalendario");
                }

                var ejercicios = JsonSerializer.Deserialize<List<EjercicioCalendarModel>>(ejerciciosJson);
                var respuestas = JsonSerializer.Deserialize<List<CalendarioRespuestaModel>>(respuestasJson);
                var config = JsonSerializer.Deserialize<CalendarioConfModel>(configJson);

                // Calcular estadísticas
                int totalCorrectas = respuestas.Count(r => r.Es_Correcto);
                int totalIncorrectas = respuestas.Count(r => !r.Es_Correcto && !string.IsNullOrEmpty(r.Respuesta_Usuario));
                int totalSinResponder = respuestas.Count(r => string.IsNullOrEmpty(r.Respuesta_Usuario));

                double porcentajeAcierto = ejercicios.Count > 0 ? (totalCorrectas * 100.0) / ejercicios.Count : 0;
                double tiempoPromedio = respuestas.Where(r => r.Tiempo_Respuesta > 0).DefaultIfEmpty().Average(r => r?.Tiempo_Respuesta ?? 0);
                double tiempoTotal = respuestas.Sum(r => r.Tiempo_Respuesta);

                var modeloResultados = new ResCalendarioViewModel
                {
                    Ejercicios = ejercicios,
                    Respuestas = respuestas,
                    Configuracion = config,
                    TotalCorrectas = totalCorrectas,
                    TotalIncorrectas = totalIncorrectas,
                    TotalSinResponder = totalSinResponder,
                    PorcentajeAcierto = porcentajeAcierto,
                    TiempoPromedio = tiempoPromedio,
                    TiempoTotal = tiempoTotal,
                    FechaPrueba = DateTime.Now
                };

                HttpContext.Session.Remove("CalendarioEjercicios");
                HttpContext.Session.Remove("CalendarioRespuestas");
                //HttpContext.Session.Remove("CalendarioConfiguracion");
                HttpContext.Session.Remove("CalendarioEjercicioActual");
                HttpContext.Session.Remove("CalendarioTiempoRestante");


                PruebasDBModel result = new PruebasDBModel
                {
                    Id_Usuario = userId.Value,
                    Total_Preguntas = ejercicios.Count,
                    Respuestas_Correctas = totalCorrectas,
                    Tiempo = TimeSpan.FromSeconds(tiempoTotal),
                    Fecha = DateTime.Now,
                    ExperienciaAdquirida = (int)porcentajeAcierto,
                    Tipo_Prueba = "Calendario Mental - " + config.TipoPrueba
                };

                bool InsertarPrueba = _pruebasDBService.GuardarPrueba(result);

                return View(modeloResultados);
            }
            catch (Exception ex)
            {
                return RedirectToAction("FormularioCalendario");
            }
        }

        public List<EjercicioCalendarModel> GenerarEjerciciosCalendario(CalendarioConfModel config)
        {
            Random _random = new Random();
            CultureInfo _cultura = new CultureInfo("es-ES");


            List<EjercicioCalendarModel> ejercicios = new List<EjercicioCalendarModel>();

            for(int i = 0; i < config.CantidadEjercicios; i++)
            {
                DateTime fechaAleatoria;

                if (config.TipoOperacion.ToLower().Contains("unico"))
                {
                    fechaAleatoria = GenerarFechaEnAnno(config.AnnoFin, _random);
                }
                else 
                {
                    fechaAleatoria = GenerarFechaEnRango(config.AnnoInicio, config.AnnoFin, _random);
                }

                EjercicioCalendarModel ejercicio = new EjercicioCalendarModel
                {
                    Id_Ejercicio = i + 1,
                    Fecha_Ejercicio = fechaAleatoria,
                    Dia_Correcto = CapitalizarPrimeraLetra(fechaAleatoria.ToString("dddd", _cultura)),
                    Fecha_Formateada = fechaAleatoria.ToString("dd 'de' MMMM 'de' yyyy", _cultura)
                };

                ejercicios.Add(ejercicio);
            }

            return ejercicios;

        }

        private DateTime GenerarFechaEnAnno(int anno, Random random) 
        {
            DateTime inicioAnno = new DateTime(anno, 1, 1);
            DateTime finAnno = new DateTime(anno, 12, 31);

            int rangoDias = (finAnno - inicioAnno).Days;
            return inicioAnno.AddDays(random.Next(0, rangoDias + 1));
        }

        private DateTime GenerarFechaEnRango(int annoInicio, int annoFin, Random random) 
        {
            DateTime inicioRango = new DateTime(annoInicio, 1, 1);
            DateTime finRango = new DateTime(annoFin, 12, 31);

            int rangoDias = (finRango - inicioRango).Days;
            return inicioRango.AddDays(random.Next(0, rangoDias + 1));
        }

        private string CapitalizarPrimeraLetra(string texto)
        {
            if (string.IsNullOrEmpty(texto))
                return texto;

            return char.ToUpper(texto[0]) + texto.Substring(1).ToLower();
        }

        [HttpGet]
        public IActionResult LimpiarCalendarioYDashboard()
        {
            HttpContext.Session.Remove("CalendarioEjercicios");
            HttpContext.Session.Remove("CalendarioRespuestas");
            HttpContext.Session.Remove("CalendarioConfiguracion");
            HttpContext.Session.Remove("CalendarioEjercicioActual");
            HttpContext.Session.Remove("CalendarioTiempoRestante");
            HttpContext.Session.Remove("CalendarioInicioTiempo");
            return RedirectToAction("Desafios", "Dashboard");
        }

        [HttpGet]
        public IActionResult RepetirEjerciciosCalendario()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("Id_Usuario");
                if (userId == null || userId == 0)
                {
                    return RedirectToAction("Inicio", "Inicio");
                }

                var configJson = HttpContext.Session.GetString("CalendarioConfiguracion");

                if (string.IsNullOrEmpty(configJson))
                {
                    return RedirectToAction("FormularioCalendario");
                }

                var config = JsonSerializer.Deserialize<CalendarioConfModel>(configJson);

                List<EjercicioCalendarModel> ejercicios = GenerarEjerciciosCalendario(config);

                List<CalendarioRespuestaModel> respuestas = ejercicios.Select(e => new CalendarioRespuestaModel
                {
                    Id_Ejercicio = e.Id_Ejercicio,
                    Respuesta_Usuario = string.Empty,
                    Es_Correcto = false,
                    Tiempo_Respuesta = 0
                }).ToList();

                HttpContext.Session.SetString("CalendarioEjercicios", JsonSerializer.Serialize(ejercicios));
                HttpContext.Session.SetString("CalendarioRespuestas", JsonSerializer.Serialize(respuestas));
                HttpContext.Session.SetInt32("CalendarioEjercicioActual", 1);
                HttpContext.Session.SetInt32("CalendarioTiempoRestante", config.TiempoLimiteMinutos * 60);
                HttpContext.Session.SetString("CalendarioInicioTiempo", DateTime.Now.ToString());

                return RedirectToAction("ConcentracionCalendario");
            }
            catch (Exception ex)
            {
                return RedirectToAction("FormularioCalendario");
            }
        }

        [HttpGet]
        public IActionResult ConcentracionCalendario()
        {
            try
            {
                var configJson = HttpContext.Session.GetString("CalendarioConfiguracion");
                if (string.IsNullOrEmpty(configJson))
                {
                    return RedirectToAction("FormularioCalendario");
                }

                var config = JsonSerializer.Deserialize<CalendarioConfModel>(configJson);
                ViewBag.TiempoMeditacion = config.TiempoConcentracion;

                return View();
            }
            catch (Exception ex)
            {
                return RedirectToAction("FormularioCalendario");
            }
        }

        [HttpGet]
        public IActionResult CompetenciaCalendario()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("Id_Usuario");
                if (userId == null || userId == 0)
                {
                    return RedirectToAction("Inicio", "Inicio");
                }

                
                var config = new CalendarioConfModel
                {
                    CantidadEjercicios = 50,
                    AnnoInicio = 1900,
                    AnnoFin = 1927,
                    TiempoLimiteMinutos = 1,
                    TiempoConcentracion = 3,
                    TipoOperacion = "rango de annos",
                    TipoPrueba = "Competencia"
                };

                List<EjercicioCalendarModel> ejercicios = GenerarEjerciciosCalendario(config);
                List<CalendarioRespuestaModel> respuestas = ejercicios.Select(e => new CalendarioRespuestaModel
                {
                    Id_Ejercicio = e.Id_Ejercicio,
                    Respuesta_Usuario = string.Empty,
                    Es_Correcto = false,
                    Tiempo_Respuesta = 0
                }).ToList();

                ViewBag.TiempoMeditacion = config.TiempoConcentracion;
                HttpContext.Session.SetString("CalendarioConfiguracion", JsonSerializer.Serialize(config));
                HttpContext.Session.SetString("CalendarioEjercicios", JsonSerializer.Serialize(ejercicios));
                HttpContext.Session.SetString("CalendarioRespuestas", JsonSerializer.Serialize(respuestas));
                HttpContext.Session.SetInt32("CalendarioEjercicioActual", 1);
                HttpContext.Session.SetInt32("CalendarioTiempoRestante", config.TiempoLimiteMinutos * 60);
                HttpContext.Session.SetString("CalendarioInicioTiempo", DateTime.Now.ToString());

                
                return RedirectToAction("ConcentracionCalendario");
            }
            catch (Exception ex)
            {
                return RedirectToAction("FormularioCalendario");
            }
        }


    }
}
