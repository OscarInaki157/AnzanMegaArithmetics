using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using AnzanMegaArithmetics.Models;
using System.Globalization;

namespace AnzanMegaArithmetics.Controllers
{
    [Authorize]
    public class FingermathController : Controller
    {
        private static readonly int[][] combinacionesManoIzquierda = new[]
        {
            new[] { 5 },
            new[] { 5, 4 },
            new[] { 5, 4, 3 },
            new[] { 5, 4, 3, 2 },
            new[] { 5, 4, 3, 2, 1 }
        };

        private static readonly int[][] combinacionesManoDerecha = new[]
        {
            new[] { 6 },
            new[] { 6, 7 },
            new[] { 6, 7, 8 },
            new[] { 6, 7, 8, 9 },
            new[] { 6, 7, 8, 9, 10 }
        };

        //Modulo Lectura FingerMath logica

        [HttpGet]
        public IActionResult LecturaFinger(string tipo, string velocidad, int? cantidad)
        {
            var modelo = new ConfLecturaFingerModel
            {
               TipoPregunta = tipo ?? "ambas",
               VelocidadPreguntas = velocidad ?? "0",
               TiempoMeditacion = 3,
               CantidadEjercicios = cantidad ?? 5
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

            if (tipoPregunta == "izquierda" || tipoPregunta == "ambas")
                dedosIzquierda = combinacionesManoIzquierda[rnd.Next(combinacionesManoIzquierda.Length)].ToList();

            if (tipoPregunta == "derecha" || tipoPregunta == "ambas")
                dedosDerecha = combinacionesManoDerecha[rnd.Next(combinacionesManoDerecha.Length)].ToList();

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
        public IActionResult ResultadoLectura(int respuesta, string respondido)
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
                RespuestaCorrecta = respuestaCorrecta
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
            TempData.Keep("Resultados");
            return View();
        }

        [HttpPost]
        public IActionResult FinalizarLectura(int respuesta, string respondido, int respuestaCorrecta)
        {
            var resultadosJson = TempData["Resultados"] as string;
            List<RLecturaFingerModel> resultados = string.IsNullOrEmpty(resultadosJson)
                ? new List<RLecturaFingerModel>()
                : JsonSerializer.Deserialize<List<RLecturaFingerModel>>(resultadosJson);

            bool respondio = respondido == "true";
            resultados.Add(new RLecturaFingerModel
            {
                RespuestaUsuario = respondio ? respuesta : -1,
                RespuestaCorrecta = respuestaCorrecta
            });

            int ejerciciosRealizados = resultados.Count;
            int cantidadEjercicios = Convert.ToInt32(TempData.Peek("CantidadEjercicios"));

            // Agrega los que faltan como no respondidos
            for (int i = ejerciciosRealizados; i < cantidadEjercicios; i++)
            {
                resultados.Add(new RLecturaFingerModel
                {
                    RespuestaUsuario = -1,
                    RespuestaCorrecta = 0
                });
            }

            TempData["Resultados"] = JsonSerializer.Serialize(resultados);
            return RedirectToAction("ResultadoLectura");
        }


        //Modulo Fingermath Escritura
        [HttpGet]
        public IActionResult EscrituraFinger(string tipo, string velocidad, int? cantidad)
        {
            var modelo = new ConfEscrituraFingerModel
            {
                TipoPregunta = tipo ?? "derecha",
                VelocidadPreguntas = velocidad ?? "0",
                TiempoMeditacion = 3,
                CantidadEjercicios = cantidad ?? 5
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
                    numeroObjetivo = rnd.Next(0, 100); // 0 a 99
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

            TempData.Keep();

            return View();
        }



        [HttpPost]
        public IActionResult ResultadoEscritura(int respuesta, string respondido)
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
                EsCorrecto = respondio && respuesta == numeroObjetivo
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
            TempData.Keep("Resultados");
            return View();
        }

        [HttpPost]
        public IActionResult FinalizarEscritura()
        {
            var resultadosJson = TempData["Resultados"] as string;
            List<REscrituraFingerModel> resultados = string.IsNullOrEmpty(resultadosJson)
                ? new List<REscrituraFingerModel>()
                : JsonSerializer.Deserialize<List<REscrituraFingerModel>>(resultadosJson);

            TempData["Resultados"] = JsonSerializer.Serialize(resultados);
            return RedirectToAction("ResultadoEscritura");
        }




    }
}