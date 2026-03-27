using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using AnzanMegaArithmetics.Models;
using System.Globalization;
using AnzanMegaArithmetics.Services;

namespace AnzanMegaArithmetics.Controllers
{
    [Authorize]
    public class FingermathController : Controller
    {
        private readonly IPruebasDBService pruebasDBService;
        public FingermathController(IPruebasDBService pruebasDBService)
        {
            this.pruebasDBService = pruebasDBService;
        }

        private static readonly int[][] combinacionesManoIzquierda = new[]
        {
            // Con pulgar
            new[] { 5 },
            new[] { 5, 4 },
            new[] { 5, 4, 3 },
            new[] { 5, 4, 3, 2 },
            new[] { 5, 4, 3, 2, 1 },
    
            // Sin pulgar
            new[] { 4 },
            new[] { 4, 3 },
            new[] { 4, 3, 2 },
            new[] { 4, 3, 2, 1 }
        };

        private static readonly int[][] combinacionesManoDerecha = new[]
        {
            // Con pulgar
            new[] { 6 },
            new[] { 6, 7 },
            new[] { 6, 7, 8 },
            new[] { 6, 7, 8, 9 },
            new[] { 6, 7, 8, 9, 10 },

            // Sin pulgar
            new[] { 7 },
            new[] { 7, 8 },
            new[] { 7, 8, 9 },
            new[] { 7, 8, 9, 10 }
        };

        //Modulo Lectura FingerMath logica

        [HttpGet]
        public IActionResult LecturaFinger(string tipo, string velocidad, int? cantidad, int? tiempoMeditacion, int? estilo)
        {
            var userId = HttpContext.Session.GetInt32("Id_Usuario");

            if (userId == null || userId == 0)
            {
                return RedirectToAction("Inicio", "Inicio");
            }

            var modelo = new ConfLecturaFingerModel
            {
               TipoPregunta = tipo ?? "ambas",
               VelocidadPreguntas = velocidad ?? "0",
               TiempoMeditacion = tiempoMeditacion ?? 3,
               CantidadEjercicios = cantidad ?? 10,
               Estilo = estilo ?? 0
            };

            return View(modelo);
        }


        [HttpPost]
        public IActionResult Concentracion(ConfLecturaFingerModel config)
        {
            config.CantidadEjercicios = Math.Max(1, config.CantidadEjercicios);
            config.TiempoMeditacion = Math.Max(0, config.TiempoMeditacion);

            TempData["TipoPregunta"] = config.TipoPregunta;
            TempData["VelocidadPreguntas"] = config.VelocidadPreguntas;
            TempData["TiempoMeditacion"] = config.TiempoMeditacion;
            TempData["CantidadEjercicios"] = config.CantidadEjercicios;
            TempData["EjerciciosRealizados"] = 0;
            TempData["Estilo"] = config.Estilo;
            TempData["Resultados"] = JsonSerializer.Serialize(new List<RLecturaFingerModel>());

            ViewBag.TiempoMeditacion = config.TiempoMeditacion;
            ViewBag.TipoPregunta = config.TipoPregunta;
            ViewBag.VelocidadPreguntas = config.VelocidadPreguntas;

            return View("Concentracion");
        }

        [HttpGet]
        public IActionResult EjercicioLectura()
        {
            string tipoPregunta = TempData["TipoPregunta"]?.ToString();
            float velocidadPreguntas = float.Parse(TempData["VelocidadPreguntas"].ToString().Replace(",", "."), CultureInfo.InvariantCulture);
            int estilo = Convert.ToInt32(TempData["Estilo"]);
            int cantidadEjercicios = Convert.ToInt32(TempData["CantidadEjercicios"]);
            int ejerciciosRealizados = Convert.ToInt32(TempData["EjerciciosRealizados"]);

            var resultadosJson = TempData["Resultados"] as string;
            List<RLecturaFingerModel> resultados = string.IsNullOrEmpty(resultadosJson)
                ? new List<RLecturaFingerModel>()
                : JsonSerializer.Deserialize<List<RLecturaFingerModel>>(resultadosJson);

            if (ejerciciosRealizados >= cantidadEjercicios)
            {
                TempData["Resultados"] = JsonSerializer.Serialize(resultados);
                return RedirectToAction("ResultadoLectura");
            }

            ViewBag.TipoPregunta = tipoPregunta;
            ViewBag.VelocidadPreguntas = velocidadPreguntas;

            Random rnd = new Random();
            List<int> dedosIzquierda = new();
            List<int> dedosDerecha = new();

            if (tipoPregunta == "ambas")
            {
                int opcion = rnd.Next(3);

                if (opcion == 0)
                {
                    dedosIzquierda = combinacionesManoIzquierda[rnd.Next(combinacionesManoIzquierda.Length)].ToList();
                }
                else if (opcion == 1)
                {
                    dedosDerecha = combinacionesManoDerecha[rnd.Next(combinacionesManoDerecha.Length)].ToList();
                }
                else
                {
                    dedosIzquierda = combinacionesManoIzquierda[rnd.Next(combinacionesManoIzquierda.Length)].ToList();
                    dedosDerecha = combinacionesManoDerecha[rnd.Next(combinacionesManoDerecha.Length)].ToList();
                }
            }
            else if (tipoPregunta == "izquierda")
            {
                dedosIzquierda = combinacionesManoIzquierda[rnd.Next(combinacionesManoIzquierda.Length)].ToList();
            }
            else if (tipoPregunta == "derecha")
            {
                dedosDerecha = combinacionesManoDerecha[rnd.Next(combinacionesManoDerecha.Length)].ToList();
            }


            //if (tipoPregunta == "izquierda" || tipoPregunta == "ambas")
            //    dedosIzquierda = combinacionesManoIzquierda[rnd.Next(combinacionesManoIzquierda.Length)].ToList();

            //if (tipoPregunta == "derecha" || tipoPregunta == "ambas")
            //    dedosDerecha = combinacionesManoDerecha[rnd.Next(combinacionesManoDerecha.Length)].ToList();

            ViewBag.DedosIzquierda = dedosIzquierda;
            ViewBag.DedosDerecha = dedosDerecha;

            int valor = 0;
            foreach (int dedo in dedosIzquierda)
                valor += (dedo == 5) ? 50 : 10;

            foreach (int dedo in dedosDerecha)
                valor += (dedo == 6) ? 5 : 1;

            ViewBag.RespuestaCorrecta = valor;
            ViewBag.EjercicioActual = ejerciciosRealizados + 1;
            ViewBag.TotalEjercicios = cantidadEjercicios;
            ViewBag.SkinId = estilo;
            TempData["RespuestaCorrecta"] = valor;
            TempData["EjerciciosRealizados"] = ejerciciosRealizados;
            TempData["Resultados"] = JsonSerializer.Serialize(resultados);
            TempData["TipoPregunta"] = tipoPregunta;
            TempData["VelocidadPreguntas"] = velocidadPreguntas.ToString(CultureInfo.InvariantCulture);
            TempData["CantidadEjercicios"] = cantidadEjercicios;

            TempData.Keep();

            return View();
        }

        [HttpPost]
        public IActionResult ResultadoLectura(int respuesta, string respondido, double tiempoRespuesta)
        {
            bool respondio = respondido == "true";
            int respuestaCorrecta = Convert.ToInt32(TempData["RespuestaCorrecta"]);
            int ejerciciosRealizados = Convert.ToInt32(TempData["EjerciciosRealizados"]);
            int cantidadEjercicios = Convert.ToInt32(TempData.Peek("CantidadEjercicios"));

            var resultadosJson = TempData["Resultados"] as string;
            List<RLecturaFingerModel> resultados = string.IsNullOrEmpty(resultadosJson)
                ? new List<RLecturaFingerModel>()
                : JsonSerializer.Deserialize<List<RLecturaFingerModel>>(resultadosJson);

            resultados.Add(new RLecturaFingerModel
            {
                RespuestaUsuario = respondio ? respuesta : -1,
                RespuestaCorrecta = respuestaCorrecta,
                TiempoRespuesta = tiempoRespuesta
            });

           
            ejerciciosRealizados++;

            TempData["Resultados"] = JsonSerializer.Serialize(resultados);
            TempData["EjerciciosRealizados"] = ejerciciosRealizados;

            TempData["TipoPregunta"] = TempData.Peek("TipoPregunta");
            TempData["VelocidadPreguntas"] = TempData.Peek("VelocidadPreguntas");
            TempData["CantidadEjercicios"] = TempData.Peek("CantidadEjercicios");

        
            if (ejerciciosRealizados >= cantidadEjercicios)
            {
                TempData["Resultados"] = JsonSerializer.Serialize(resultados);
                return RedirectToAction("ResultadoLectura");
            }

            return RedirectToAction("EjercicioLectura");
        }

        [HttpGet]
        public IActionResult ResultadoLectura()
        {
            var userId = HttpContext.Session.GetInt32("Id_Usuario");
            if (userId == null || userId == 0)
            {
                return RedirectToAction("Inicio", "Inicio");
            }

            var resultados = JsonSerializer.Deserialize<List<RLecturaFingerModel>>(TempData["Resultados"] as string) ?? new List<RLecturaFingerModel>();

            string tipoPregunta = TempData.Peek("TipoPregunta")?.ToString() ?? "ambas";
            string velocidadPreguntas = TempData.Peek("VelocidadPreguntas")?.ToString() ?? "0";
            int cantidadEjercicios = Convert.ToInt32(TempData.Peek("CantidadEjercicios"));
            int tiempoMeditacion = Convert.ToInt32(TempData.Peek("TiempoMeditacion") ?? 3);
            int estilo = Convert.ToInt32(TempData.Peek("Estilo") ?? 0);

            int correctos = resultados.Count(r => r.RespuestaUsuario == r.RespuestaCorrecta);
            int incorrectos = resultados.Count(r => r.RespuestaUsuario != -1 && r.RespuestaUsuario != r.RespuestaCorrecta);
            int porcentaje = cantidadEjercicios > 0 ? (correctos * 100) / cantidadEjercicios : 0;
            int xp = porcentaje;
            double tiempoTotal = resultados.Sum(r => r.TiempoRespuesta);

            var configParaGuardar = new ConfLecturaFingerModel
            {
                TipoPregunta = tipoPregunta,
                VelocidadPreguntas = velocidadPreguntas,
                CantidadEjercicios = cantidadEjercicios,
                TiempoMeditacion = tiempoMeditacion,
                Estilo = estilo
            };
            string jsonConfiguracion = JsonSerializer.Serialize(configParaGuardar);

            //armar modelo generico para resultados
            PruebasDBModel results = new PruebasDBModel
            {
                Id_Usuario = userId.Value,
                Total_Preguntas = cantidadEjercicios,
                Respuestas_Correctas = correctos,
                Fecha = DateTime.Now,
                Tipo_Prueba = "Fingermath Lectura",
                ExperienciaAdquirida = xp,
                Tiempo = TimeSpan.FromSeconds(tiempoTotal),
                Configuracion = jsonConfiguracion
            };

            bool InsertarPrueba = pruebasDBService.GuardarPrueba(results);

            ViewBag.TipoPregunta = tipoPregunta;
            ViewBag.VelocidadPreguntas = velocidadPreguntas;
            ViewBag.CantidadEjercicios = cantidadEjercicios;
            ViewBag.TiempoMeditacion = tiempoMeditacion;
            ViewBag.Estilo = estilo;

            TempData.Keep("Resultados");
            return View();
        }

        [HttpPost]
        public IActionResult FinalizarLectura(int respuesta, string respondido, int respuestaCorrecta, double tiempoRespuesta)
        {
            var resultadosJson = TempData["Resultados"] as string;
            List<RLecturaFingerModel> resultados = string.IsNullOrEmpty(resultadosJson)
                ? new List<RLecturaFingerModel>()
                : JsonSerializer.Deserialize<List<RLecturaFingerModel>>(resultadosJson);

            bool respondio = respondido == "true";
            resultados.Add(new RLecturaFingerModel
            {
                RespuestaUsuario = respondio ? respuesta : -1,
                RespuestaCorrecta = respuestaCorrecta,
                TiempoRespuesta = tiempoRespuesta
            });

            int ejerciciosRealizados = resultados.Count;
            int cantidadEjercicios = Convert.ToInt32(TempData.Peek("CantidadEjercicios"));

            // Agrega los que faltan como no respondidos
            for (int i = ejerciciosRealizados; i < cantidadEjercicios; i++)
            {
                resultados.Add(new RLecturaFingerModel
                {
                    RespuestaUsuario = -1,
                    RespuestaCorrecta = 0,
                    TiempoRespuesta = 0
                });
            }

            TempData["Resultados"] = JsonSerializer.Serialize(resultados);
            return RedirectToAction("ResultadoLectura");
        }


        //Modulo Fingermath Escritura
        [HttpGet]
        public IActionResult EscrituraFinger(string tipo, string velocidad, int? cantidad, int? tiempoMeditacion, int? estilo)
        {
            var userId = HttpContext.Session.GetInt32("Id_Usuario");

            if (userId == null || userId == 0)
            {
                return RedirectToAction("Inicio", "Inicio");
            }

            var modelo = new ConfEscrituraFingerModel
            {
                TipoPregunta = tipo ?? "ambas",
                VelocidadPreguntas = velocidad ?? "0",
                TiempoMeditacion = tiempoMeditacion ?? 3,
                CantidadEjercicios = cantidad ?? 10,
                Estilo = estilo ?? 0
            };

            return View(modelo);
        }


        [HttpPost]
        public IActionResult ConcentracionEscritura(ConfEscrituraFingerModel config)
        {
            config.CantidadEjercicios = Math.Max(1, config.CantidadEjercicios);
            config.TiempoMeditacion = Math.Max(0, config.TiempoMeditacion);

            TempData["TipoPregunta"] = config.TipoPregunta;
            TempData["VelocidadPreguntas"] = config.VelocidadPreguntas;
            TempData["TiempoMeditacion"] = config.TiempoMeditacion;
            TempData["CantidadEjercicios"] = config.CantidadEjercicios;
            TempData["EjerciciosRealizados"] = 0;
            TempData["Resultados"] = JsonSerializer.Serialize(new List<REscrituraFingerModel>());
            TempData["Estilo"] = config.Estilo;
            ViewBag.TiempoMeditacion = config.TiempoMeditacion;
            ViewBag.TipoPregunta = config.TipoPregunta;
            ViewBag.VelocidadPreguntas = config.VelocidadPreguntas;

            return View();
        }

        [HttpGet]
        public IActionResult EjercicioEscritura()
        {
            string tipoPregunta = TempData["TipoPregunta"]?.ToString();
            float velocidadPreguntas = float.Parse(TempData["VelocidadPreguntas"].ToString().Replace(",", "."), CultureInfo.InvariantCulture);
            int estilo = Convert.ToInt32(TempData["Estilo"]);
            int cantidadEjercicios = Convert.ToInt32(TempData["CantidadEjercicios"]);
            int ejerciciosRealizados = Convert.ToInt32(TempData["EjerciciosRealizados"]);

            var resultadosJson = TempData["Resultados"] as string;
            List<REscrituraFingerModel> resultados = string.IsNullOrEmpty(resultadosJson)
                ? new List<REscrituraFingerModel>()
                : JsonSerializer.Deserialize<List<REscrituraFingerModel>>(resultadosJson);

            if (ejerciciosRealizados >= cantidadEjercicios)
            {
                TempData["Resultados"] = JsonSerializer.Serialize(resultados);
                return RedirectToAction("ResultadoEscritura");
            }

            ViewBag.TipoPregunta = tipoPregunta;
            ViewBag.VelocidadPreguntas = velocidadPreguntas;

            Random rnd = new Random();
            int numeroObjetivo = 0;

            switch (tipoPregunta)
            {
                case "izquierda":
                    numeroObjetivo = rnd.Next(0, 10) * 10; // 0, 10, 20, ..., 90
                    break;
                case "derecha":
                    numeroObjetivo = rnd.Next(0, 10); // 0 a 9
                    break;
                case "ambas":
                    int opcion = rnd.Next(3);
                    if (opcion == 0)
                        numeroObjetivo = rnd.Next(1, 10) * 10; // 10, 20, ..., 90
                    else if (opcion == 1)
                        numeroObjetivo = rnd.Next(1, 10); // 1 a 9
                    else
                        numeroObjetivo = rnd.Next(1, 100); // 1 a 99 (ambas manos)
                    break;
                default:
                    numeroObjetivo = 0;
                    break;
            }

            ViewBag.NumeroObjetivo = numeroObjetivo;
            ViewBag.EjercicioActual = ejerciciosRealizados + 1;
            ViewBag.TotalEjercicios = cantidadEjercicios;

            TempData["NumeroObjetivo"] = numeroObjetivo;
            TempData["EjerciciosRealizados"] = ejerciciosRealizados;
            TempData["Resultados"] = JsonSerializer.Serialize(resultados);
            TempData["TipoPregunta"] = tipoPregunta;
            TempData["VelocidadPreguntas"] = velocidadPreguntas.ToString(CultureInfo.InvariantCulture);

            TempData["CantidadEjercicios"] = cantidadEjercicios;
            ViewBag.SkinId = estilo;
            TempData.Keep();

            return View();
        }

        [HttpPost]
        public IActionResult ResultadoEscritura(int respuesta, string respondido, double tiempoRespuesta)
        {
            bool respondio = respondido == "true";
            int numeroObjetivo = Convert.ToInt32(TempData["NumeroObjetivo"]);
            int ejerciciosRealizados = Convert.ToInt32(TempData["EjerciciosRealizados"]);

            var resultadosJson = TempData["Resultados"] as string;
            List<REscrituraFingerModel> resultados = string.IsNullOrEmpty(resultadosJson)
                ? new List<REscrituraFingerModel>()
                : JsonSerializer.Deserialize<List<REscrituraFingerModel>>(resultadosJson);

            resultados.Add(new REscrituraFingerModel
            {
                RespuestaUsuario = respondio ? respuesta : -1,
                RespuestaCorrecta = numeroObjetivo,
                EsCorrecto = respondio && respuesta == numeroObjetivo,
                TiempoRespuesta = tiempoRespuesta
            });

            ejerciciosRealizados++;

            TempData["Resultados"] = JsonSerializer.Serialize(resultados);
            TempData["EjerciciosRealizados"] = ejerciciosRealizados;

            // Preservar los valores necesarios para continuar con los siguientes ejercicios
            TempData["TipoPregunta"] = TempData.Peek("TipoPregunta");
            TempData["VelocidadPreguntas"] = TempData.Peek("VelocidadPreguntas");
            TempData["CantidadEjercicios"] = TempData.Peek("CantidadEjercicios");

            return RedirectToAction("EjercicioEscritura");
        }

        [HttpGet]
        public IActionResult ResultadoEscritura()
        {
            var userId = HttpContext.Session.GetInt32("Id_Usuario");

            if (userId == null || userId == 0)
            {
                return RedirectToAction("Inicio", "Inicio");
            }

            var resultados = JsonSerializer.Deserialize<List<REscrituraFingerModel>>(TempData["Resultados"] as string) ?? new List<REscrituraFingerModel>();

            string tipoPregunta = TempData.Peek("TipoPregunta")?.ToString() ?? "ambas";
            string velocidadPreguntas = TempData.Peek("VelocidadPreguntas")?.ToString() ?? "0";
            int cantidadEjercicios = Convert.ToInt32(TempData.Peek("CantidadEjercicios"));
            int tiempoMeditacion = Convert.ToInt32(TempData.Peek("TiempoMeditacion") ?? 3);
            int estilo = Convert.ToInt32(TempData.Peek("Estilo") ?? 0);

            int correctos = resultados.Count(r => r.RespuestaUsuario == r.RespuestaCorrecta);
            int incorrectos = resultados.Count(r => r.RespuestaUsuario != -1 && r.RespuestaUsuario != r.RespuestaCorrecta);
            int porcentaje = cantidadEjercicios > 0 ? (correctos * 100) / cantidadEjercicios : 0;
            int xp = porcentaje;
            double tiempoTotal = resultados.Sum(r => r.TiempoRespuesta);

            var configParaGuardar = new ConfEscrituraFingerModel
            {
                TipoPregunta = tipoPregunta,
                VelocidadPreguntas = velocidadPreguntas,
                CantidadEjercicios = cantidadEjercicios,
                TiempoMeditacion = tiempoMeditacion,
                Estilo = estilo
            };
            string jsonConfiguracion = JsonSerializer.Serialize(configParaGuardar);

            //armar modelo generico para resultados
            PruebasDBModel results = new PruebasDBModel
            {
                Id_Usuario = userId.Value,
                Total_Preguntas = cantidadEjercicios,
                Respuestas_Correctas = correctos,
                Fecha = DateTime.Now,
                Tipo_Prueba = "Fingermath Escritura",
                ExperienciaAdquirida = xp,
                Tiempo = TimeSpan.FromSeconds(tiempoTotal),
                Configuracion = jsonConfiguracion
            };

            bool InsertarPrueba = pruebasDBService.GuardarPrueba(results);

            ViewBag.TipoPregunta = tipoPregunta;
            ViewBag.VelocidadPreguntas = velocidadPreguntas;
            ViewBag.CantidadEjercicios = cantidadEjercicios;
            ViewBag.TiempoMeditacion = tiempoMeditacion;
            ViewBag.Estilo = estilo;

            TempData.Keep("Resultados");
            return View();
        }


        [HttpPost]
        public IActionResult FinalizarEscritura(int respuesta, string respondido, int respuestaCorrecta, double tiempoRespuesta)
        {
            var resultadosJson = TempData["Resultados"] as string;
            List<REscrituraFingerModel> resultados = string.IsNullOrEmpty(resultadosJson)
                ? new List<REscrituraFingerModel>()
                : JsonSerializer.Deserialize<List<REscrituraFingerModel>>(resultadosJson);

            bool respondio = respondido == "true";
            resultados.Add(new REscrituraFingerModel
            {
                RespuestaUsuario = respondio ? respuesta : -1,
                RespuestaCorrecta = respuestaCorrecta,
                EsCorrecto = respondio && respuesta == respuestaCorrecta,
                TiempoRespuesta = tiempoRespuesta
            });

            int ejerciciosRealizados = resultados.Count;
            int cantidadEjercicios = Convert.ToInt32(TempData.Peek("CantidadEjercicios"));

            for (int i = ejerciciosRealizados; i < cantidadEjercicios; i++)
            {
                resultados.Add(new REscrituraFingerModel
                {
                    RespuestaUsuario = -1,
                    RespuestaCorrecta = 0,
                    EsCorrecto = false,
                    TiempoRespuesta = 0
                });
            }

            TempData["Resultados"] = JsonSerializer.Serialize(resultados);
            return RedirectToAction("ResultadoEscritura");
        }

        public IActionResult RegresarDashboard()
        {
            return RedirectToAction("Dashboard", "Dashboard");
        }

    }
}