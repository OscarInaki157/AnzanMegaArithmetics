using AnzanMegaArithmetics.Models;
using AnzanMegaArithmetics.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AnzanMegaArithmetics.Controllers
{
    [Authorize]
    public class PotenciasController : Controller
    {
        private readonly IPruebasDBService _pruebasDBService;

        public PotenciasController(IPruebasDBService pruebasDBService)
        {
            this._pruebasDBService = pruebasDBService;
        }

        [HttpGet]
        public IActionResult LimpiarYDashboard() 
        {
            HttpContext.Session.Remove("EjerciciosPotencias");
            HttpContext.Session.Remove("RespuestasPotencias");
            //HttpContext.Session.Remove("ConfPotencias");
            HttpContext.Session.Remove("EjercicioActualPotencias");
            HttpContext.Session.Remove("TiempoRestantePotencias");
            return RedirectToAction("Desafios", "Dashboard");
        }

        [HttpGet]
        public IActionResult FormularioPotencias()
        {
            var userId = HttpContext.Session.GetInt32("Id_Usuario");

            if (userId == null || userId == 0)
            {
                return RedirectToAction("Inicio", "Inicio");
            }

            ConfPotenciasModel config;
            var configJson = HttpContext.Session.GetString("ConfPotencias");

            if (string.IsNullOrEmpty(configJson))
            {

                config = new ConfPotenciasModel
                {
                    CantidadEjercicios = 10,
                    NumeroInicial = 11,
                    NumeroFinal = 19,
                    RangosSeleccionados = "11-19",
                    TiempoTotal = 5,
                    TiempoMeditacion = 3,
                    TipoPrueba = "Práctica"
                };


                HttpContext.Session.SetString("ConfPotencias", JsonSerializer.Serialize(config));
            }
            else
            {
                config = JsonSerializer.Deserialize<ConfPotenciasModel>(configJson);
                config.TipoPrueba = "Práctica";
            }

            return View(config);
        }

        [HttpPost]
        public IActionResult ConcentracionPotencias(ConfPotenciasModel config) 
        {
            try
            {
                //generar ejercicios
                List<EjercicioPotenciasModel> ejercicios = GenerarEjerciciosPotencias(config);

                //generar respuestas
                List<RespuestaPotenciaModel> respuestas = ejercicios.Select(e => new RespuestaPotenciaModel
                {
                    Id_Ejercicio = e.Id_Ejercicio,
                    Respuesta_Usuario = 0,
                    Es_Correcto = false,
                    Tiempo_Respuesta = 0
                }).ToList();

                //guardar en session
                HttpContext.Session.SetString("ConfPotencias", JsonSerializer.Serialize(config));
                HttpContext.Session.SetString("EjerciciosPotencias", JsonSerializer.Serialize(ejercicios));
                HttpContext.Session.SetString("RespuestasPotencias", JsonSerializer.Serialize(respuestas));
                HttpContext.Session.SetInt32("EjercicioActualPotencias", 1);

                HttpContext.Session.SetInt32("TiempoRestantePotencias", config.TiempoTotal * 60);
                HttpContext.Session.SetString("InicioTiempoPotencias", DateTime.Now.ToString());

                ViewBag.TiempoMeditacion = config.TiempoMeditacion;
                return View();
            }
            catch (Exception ex)
            {
                return RedirectToAction("FormularioPotencias");
            }
        }

        private List<EjercicioPotenciasModel> GenerarEjerciciosPotencias(ConfPotenciasModel config)
        {
            List<EjercicioPotenciasModel> ejercicios = new();
            Random rand = new Random();

            // 2.1 Convertir el string "11-19,31-69" en una lista de tuplas matemáticas (min, max)
            List<(int min, int max)> rangosDisponibles = new List<(int, int)>();

            // Si por alguna razón RangosSeleccionados está vacío, usamos el 11-19 por defecto
            string rangosString = string.IsNullOrEmpty(config.RangosSeleccionados)
                                    ? "11-19"
                                    : config.RangosSeleccionados;

            var paresDeRangos = rangosString.Split(',');
            foreach (var par in paresDeRangos)
            {
                var limites = par.Split('-');
                if (limites.Length == 2 && int.TryParse(limites[0], out int min) && int.TryParse(limites[1], out int max))
                {
                    rangosDisponibles.Add((min, max));
                }
            }

            // Fallback por si todos los rangos fallaron
            if (!rangosDisponibles.Any()) rangosDisponibles.Add((11, 19));

            // 2.2 Crear los ejercicios saltando entre los rangos seleccionados al azar
            for (int i = 0; i < config.CantidadEjercicios; i++)
            {
                // Elige un rango al azar de los seleccionados
                var rangoElegido = rangosDisponibles[rand.Next(rangosDisponibles.Count)];

                // Genera el número base dentro de ese rango
                int numeroBase = rand.Next(rangoElegido.min, rangoElegido.max + 1);

                EjercicioPotenciasModel ejercicio = new EjercicioPotenciasModel
                {
                    Id_Ejercicio = i + 1,
                    Numero_Base = numeroBase
                };
                ejercicios.Add(ejercicio);
            }

            return ejercicios;
        }

        [HttpGet]
        public IActionResult EjercicioPotencias()
        {
            try
            {
                var configJson = HttpContext.Session.GetString("ConfPotencias");
                var ejerciciosJson = HttpContext.Session.GetString("EjerciciosPotencias");

                if (string.IsNullOrEmpty(ejerciciosJson) || string.IsNullOrEmpty(configJson))
                {
                    return RedirectToAction("FormularioPotencias");
                }

                List<EjercicioPotenciasModel> ejercicios = JsonSerializer.Deserialize<List<EjercicioPotenciasModel>>(ejerciciosJson);
                ConfPotenciasModel config = JsonSerializer.Deserialize<ConfPotenciasModel>(configJson);
                int ejercicioActual = HttpContext.Session.GetInt32("EjercicioActualPotencias") ?? 1;

                if (ejercicioActual > ejercicios.Count())
                {
                    return RedirectToAction("ResultadosPotencias");
                }

                
                int tiempoRestante = HttpContext.Session.GetInt32("TiempoRestantePotencias") ?? 0;

                if (tiempoRestante <= 0)
                {
                    return RedirectToAction("ResultadosPotencias");
                }

                EjercicioPotenciasModel ejercicio = ejercicios.FirstOrDefault(e => e.Id_Ejercicio == ejercicioActual);
                if (ejercicio == null)
                {
                    return RedirectToAction("FormularioPotencias");
                }

                ViewBag.EjercicioActual = ejercicioActual;
                ViewBag.TotalEjercicios = ejercicios.Count();
                ViewBag.TiempoRestante = tiempoRestante;
                return View(ejercicio);
            }
            catch (Exception ex)
            {
                return RedirectToAction("FormularioPotencias");
            }
        }

        [HttpPost]
        public IActionResult EjercicioPotencias(double respuestaUsuario, double tiempoRespuesta)
        {
            try
            {
                var ejerciciosJson = HttpContext.Session.GetString("EjerciciosPotencias");
                var respuestasJson = HttpContext.Session.GetString("RespuestasPotencias");
                var ejercicioActual = HttpContext.Session.GetInt32("EjercicioActualPotencias") ?? 1;
                var tiempoRestante = HttpContext.Session.GetInt32("TiempoRestantePotencias") ?? 0;

                if (string.IsNullOrEmpty(ejerciciosJson) || string.IsNullOrEmpty(respuestasJson))
                {
                    return RedirectToAction("FormularioPotencias");
                }

                List<EjercicioPotenciasModel> ejercicios = JsonSerializer.Deserialize<List<EjercicioPotenciasModel>>(ejerciciosJson);
                List<RespuestaPotenciaModel> respuestas = JsonSerializer.Deserialize<List<RespuestaPotenciaModel>>(respuestasJson);

                var ejercicio = ejercicios.FirstOrDefault(e => e.Id_Ejercicio == ejercicioActual);
                var respuesta = respuestas.FirstOrDefault(r => r.Id_Ejercicio == ejercicioActual);

                if (ejercicio == null || respuesta == null)
                {
                    return RedirectToAction("FormularioPotencias");
                }

                respuesta.Respuesta_Usuario = respuestaUsuario;
                respuesta.Es_Correcto = Math.Abs(respuestaUsuario - ejercicio.Respuesta_Correcta) < 0.0001;
                respuesta.Tiempo_Respuesta = tiempoRespuesta;

                HttpContext.Session.SetString("RespuestasPotencias", JsonSerializer.Serialize(respuestas));

                
                if (tiempoRestante > 0)
                {
                    tiempoRestante = Math.Max(0, tiempoRestante - (int)Math.Ceiling(tiempoRespuesta));
                }

                HttpContext.Session.SetInt32("TiempoRestantePotencias", tiempoRestante);
                HttpContext.Session.SetInt32("EjercicioActualPotencias", ejercicioActual + 1);

                
                if (ejercicioActual + 1 > ejercicios.Count || tiempoRestante <= 0)
                {
                    return RedirectToAction("ResultadosPotencias");
                }
                else
                {
                    return RedirectToAction("EjercicioPotencias");
                }
            }
            catch (Exception ex)
            {
                return RedirectToAction("FormularioPotencias");
            }
        }

        [HttpGet]
        public IActionResult ResultadosPotencias()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("Id_Usuario");
                if (userId == null || userId == 0)
                {
                    return RedirectToAction("Inicio", "Inicio");
                }

                //obtener datos de session
                var ejerciciosJson = HttpContext.Session.GetString("EjerciciosPotencias");
                var respuestasJson = HttpContext.Session.GetString("RespuestasPotencias");
                var configJson = HttpContext.Session.GetString("ConfPotencias");

                if (string.IsNullOrEmpty(ejerciciosJson) || string.IsNullOrEmpty(respuestasJson) || string.IsNullOrEmpty(configJson))
                {
                    return RedirectToAction("FormularioPotencias");
                }

                var ejercicios = JsonSerializer.Deserialize<List<EjercicioPotenciasModel>>(ejerciciosJson);
                var respuestas = JsonSerializer.Deserialize<List<RespuestaPotenciaModel>>(respuestasJson);
                var config = JsonSerializer.Deserialize<ConfPotenciasModel>(configJson);

                //Calcular resultados
                int correctas = respuestas.Count(r => r.Es_Correcto);
                int incorrectas = respuestas.Count(r => !r.Es_Correcto);
                int sinResponder = ejercicios.Count - respuestas.Count(r => r.Respuesta_Usuario > 0);

                double porcentajeAcierto = ejercicios.Count > 0 ? (correctas * 100.0) / ejercicios.Count : 0;
                double tiempoPromedio = respuestas.Where(r => r.Tiempo_Respuesta > 0).DefaultIfEmpty().Average(r => r?.Tiempo_Respuesta ?? 0);
                double tiempoTotal = respuestas.Sum(r => r.Tiempo_Respuesta);

                var modeloResultados = new ResPotenciasViewModel
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

                HttpContext.Session.Remove("EjerciciosPotencias");
                HttpContext.Session.Remove("RespuestasPotencias");
                HttpContext.Session.Remove("EjercicioActualPotencias");
                HttpContext.Session.Remove("TiempoRestantePotencias");

                PruebasDBModel result = new PruebasDBModel
                {
                    Id_Usuario = userId.Value,
                    Total_Preguntas = ejercicios.Count,
                    Respuestas_Correctas = correctas,
                    Tiempo = TimeSpan.FromSeconds(tiempoTotal),
                    Fecha = DateTime.Now,
                    ExperienciaAdquirida = (int)porcentajeAcierto,
                    Tipo_Prueba = "Potencias - " + config.TipoPrueba,
                    Configuracion = configJson ?? "No se pudo recuperar la configuración del servidor"
                };

                bool InsertarPrueba = _pruebasDBService.GuardarPrueba(result);

                return View(modeloResultados);
            }
            catch (Exception ex)
            {
                return RedirectToAction("FormularioPotencias");
            }

        }

        [HttpGet]
        public IActionResult RepetirPotencias()
        {
            try
            {
                
                var configJson = HttpContext.Session.GetString("ConfPotencias");

                if (string.IsNullOrEmpty(configJson))
                {
                  
                    return RedirectToAction("FormularioPotencias");
                }

             
                var config = JsonSerializer.Deserialize<ConfPotenciasModel>(configJson);

                List<EjercicioPotenciasModel> ejercicios = GenerarEjerciciosPotencias(config);

                List<RespuestaPotenciaModel> respuestas = ejercicios.Select(e => new RespuestaPotenciaModel
                {
                    Id_Ejercicio = e.Id_Ejercicio,
                    Respuesta_Usuario = 0,
                    Es_Correcto = false,
                    Tiempo_Respuesta = 0
                }).ToList();

                HttpContext.Session.SetString("EjerciciosPotencias", JsonSerializer.Serialize(ejercicios));
                HttpContext.Session.SetString("RespuestasPotencias", JsonSerializer.Serialize(respuestas));
                HttpContext.Session.SetInt32("EjercicioActualPotencias", 1);

                HttpContext.Session.SetInt32("TiempoRestantePotencias", config.TiempoTotal * 60);
                HttpContext.Session.SetString("InicioTiempoPotencias", DateTime.Now.ToString());

                ViewBag.TiempoMeditacion = config.TiempoMeditacion;
                return View("ConcentracionPotencias");
            }
            catch (Exception ex)
            {
                return RedirectToAction("FormularioPotencias");
            }
        }


        [HttpGet]
        public IActionResult CompetenciaPotencias()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("Id_Usuario");
                if (userId == null || userId == 0)
                {
                    return RedirectToAction("Inicio", "Inicio");
                }

                var config = new ConfPotenciasModel
                {
                    CantidadEjercicios = 50,
                    NumeroInicial = 10,
                    NumeroFinal = 130,
                    RangosSeleccionados = "10-130",
                    TiempoTotal = 1,
                    TiempoMeditacion = 3,
                    TipoPrueba = "Competencia"
                };

                // Generar ejercicios
                List<EjercicioPotenciasModel> ejercicios = GenerarEjerciciosPotencias(config);

                // Generar respuestas
                List<RespuestaPotenciaModel> respuestas = ejercicios.Select(e => new RespuestaPotenciaModel
                {
                    Id_Ejercicio = e.Id_Ejercicio,
                    Respuesta_Usuario = 0,
                    Es_Correcto = false,
                    Tiempo_Respuesta = 0
                }).ToList();

                // Guardar en session
                HttpContext.Session.SetString("ConfPotencias", JsonSerializer.Serialize(config));
                HttpContext.Session.SetString("EjerciciosPotencias", JsonSerializer.Serialize(ejercicios));
                HttpContext.Session.SetString("RespuestasPotencias", JsonSerializer.Serialize(respuestas));
                HttpContext.Session.SetInt32("EjercicioActualPotencias", 1);

                HttpContext.Session.SetInt32("TiempoRestantePotencias", config.TiempoTotal * 60);
                HttpContext.Session.SetString("InicioTiempoPotencias", DateTime.Now.ToString());

                ViewBag.TiempoMeditacion = config.TiempoMeditacion;
                return View("ConcentracionPotencias");
            }
            catch (Exception ex)
            {
                return RedirectToAction("FormularioPotencias");
            }
        }
    }
}
