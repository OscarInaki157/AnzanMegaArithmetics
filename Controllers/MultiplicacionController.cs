using AnzanMegaArithmetics.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AnzanMegaArithmetics.Controllers
{
    [Authorize]
    public class MultiplicacionController : Controller
    {
        [HttpGet]
        public IActionResult TablasForm()
        {
            var configStr = HttpContext.Session.GetString("ConfTablas");

            ConfTablasModel modelo;

            if (!string.IsNullOrEmpty(configStr))
            {
                try
                {
                    modelo = JsonSerializer.Deserialize<ConfTablasModel>(configStr);
                    modelo.CantidadEjercicios = 1;
                }
                catch
                {
                    modelo = ConfigDef();
                }
            }
            else
            {
                modelo = ConfigDef();
            }
            return View(modelo);
        }

        private ConfTablasModel ConfigDef()
        {
            return new ConfTablasModel
            {
                CantidadEjercicios = 5,
                VelocidadPreguntas = "0.0",
                TiempoMeditacion = 3,
                TipoPregunta = "1",
                ParImpar = "ambos",
                DigitosMultiplicacion = "1,2,3,4,5,6,7,8,9"
            };
        }

        [HttpPost]
        public IActionResult ConcentracionTablas(ConfTablasModel config)
        {
            // Restaurar TempData desde la nueva config
            TempData["CantidadEjercicios"] = config.CantidadEjercicios;
            TempData["TipoPregunta"] = config.TipoPregunta;
            TempData["ParImpar"] = config.ParImpar;
            TempData["VelocidadPreguntas"] = config.VelocidadPreguntas.Replace(",", ".");
            TempData["TiempoMeditacion"] = config.TiempoMeditacion;
            TempData["DigitosMultiplicacion"] = config.DigitosMultiplicacion;

            // Guardar en sesión
            HttpContext.Session.SetString("ConfTablas", JsonSerializer.Serialize(config));

            // Reiniciar el conteo
            TempData["EjerciciosRealizados"] = 0;
            TempData["Resultados"] = JsonSerializer.Serialize(new List<RMultiplicacionModel>());

            ViewBag.TiempoMeditacion = config.TiempoMeditacion;
            ViewBag.VelocidadPreguntas = config.VelocidadPreguntas.Replace(",", ".");

            return View("ConcentracionTablas");
        }

        [HttpGet]
        public IActionResult ConcentracionTablas()
        {
            var configStr = HttpContext.Session.GetString("ConfTablas");
            if (string.IsNullOrEmpty(configStr))
                return RedirectToAction("TablasForm");

            var config = JsonSerializer.Deserialize<ConfTablasModel>(configStr);

            TempData["CantidadEjercicios"] = config.CantidadEjercicios;
            TempData["TipoPregunta"] = config.TipoPregunta;
            TempData["ParImpar"] = config.ParImpar;
            TempData["VelocidadPreguntas"] = config.VelocidadPreguntas.Replace(",", ".");
            TempData["TiempoMeditacion"] = config.TiempoMeditacion;
            TempData["DigitosMultiplicacion"] = config.DigitosMultiplicacion;

            TempData["EjerciciosRealizados"] = 0;
            TempData["Resultados"] = JsonSerializer.Serialize(new List<RMultiplicacionModel>());

            ViewBag.TiempoMeditacion = config.TiempoMeditacion;
            ViewBag.VelocidadPreguntas = config.VelocidadPreguntas.Replace(",", ".");

            return View("ConcentracionTablas");
        }


        [HttpGet]
        public IActionResult EjercicioTablas()
        {
            int total = Convert.ToInt32(TempData["CantidadEjercicios"]);
            int actual = TempData.ContainsKey("EjerciciosRealizados") ? Convert.ToInt32(TempData["EjerciciosRealizados"]) : 0;

            if (actual >= total)
            {
                return RedirectToAction("ResultadoTablas");
            }

            string tipo = TempData["TipoPregunta"].ToString();
            string parImpar = TempData["ParImpar"].ToString();
            string velocidad = TempData["VelocidadPreguntas"].ToString();
            string digitosStr = TempData["DigitosMultiplicacion"].ToString();

            var resultadosJson = TempData["Resultados"] as string;
            var resultados = string.IsNullOrEmpty(resultadosJson)
                ? new List<RMultiplicacionModel>()
                : JsonSerializer.Deserialize<List<RMultiplicacionModel>>(resultadosJson);

            var random = new Random();
            var digitosPermitidos = digitosStr.Split(',').Select(d => d.Trim()).Where(d => d.Length == 1).ToArray();

            int longitud = int.Parse(tipo);
            var posibles = GenerarMultiplicandos(digitosPermitidos, longitud, parImpar);

            if (!posibles.Any())
            {
                posibles = digitosPermitidos.Select(d => int.Parse(d)).ToList();
            }

            int multiplicando = posibles[random.Next(posibles.Count)];
            int multiplicador = random.Next(1, 10); // 1 a 9

            int resultado = multiplicando * multiplicador;

            TempData["Multiplicando"] = multiplicando;
            TempData["Multiplicador"] = multiplicador;
            TempData["EjerciciosRealizados"] = actual;
            TempData["Resultados"] = JsonSerializer.Serialize(resultados);

            TempData.Keep();

            ViewBag.Multiplicando = multiplicando;
            ViewBag.Multiplicador = multiplicador;
            ViewBag.VelocidadPreguntas = velocidad;
            ViewBag.EjercicioActual = actual + 1;
            ViewBag.TotalEjercicios = total;

            return View();
        }

        private List<int> GenerarMultiplicandos(string[] digitos, int longitud, string parImpar)
        {
            var resultados = new HashSet<int>();
            var combinaciones = ProductoCartesiano(digitos, longitud);

            foreach (var combinacion in combinaciones)
            {
                var numStr = string.Concat(combinacion);
                if (numStr.StartsWith("0")) continue;

                int num = int.Parse(numStr);
                if (parImpar == "par" && num % 2 != 0) continue;
                if (parImpar == "impar" && num % 2 == 0) continue;

                resultados.Add(num);
            }

            return resultados.ToList();
        }

        private IEnumerable<IEnumerable<string>> ProductoCartesiano(string[] elementos, int longitud)
        {
            IEnumerable<IEnumerable<string>> resultado = new[] { Enumerable.Empty<string>() };

            for (int i = 0; i < longitud; i++)
            {
                resultado = resultado.SelectMany(seq => elementos.Select(e => seq.Append(e)));
            }

            return resultado;
        }

        [HttpPost]
        public IActionResult EnviarRespuestaTablas(int? respuestaUsuario)
        {
            int multiplicando = Convert.ToInt32(TempData["Multiplicando"]);
            int multiplicador = Convert.ToInt32(TempData["Multiplicador"]);
            int total = Convert.ToInt32(TempData["CantidadEjercicios"]);
            int actual = TempData.ContainsKey("EjerciciosRealizados") ? Convert.ToInt32(TempData["EjerciciosRealizados"]) : 0;

            int respuestaCorrecta = multiplicando * multiplicador;

            var resultadosJson = TempData["Resultados"] as string;
            var resultados = string.IsNullOrEmpty(resultadosJson)
                ? new List<RMultiplicacionModel>()
                : JsonSerializer.Deserialize<List<RMultiplicacionModel>>(resultadosJson);

            var model = new RMultiplicacionModel
            {
                Multiplicando = multiplicando,
                Multiplicador = multiplicador,
                RespuestaUsuario = respuestaUsuario ?? -1
            };

            resultados.Add(model);

            TempData["Resultados"] = JsonSerializer.Serialize(resultados);
            TempData["EjerciciosRealizados"] = actual + 1;

            TempData.Keep();

            if (actual + 1 >= total)
                return RedirectToAction("ResultadoTablas");

            return RedirectToAction("EjercicioTablas");
        }


        [HttpPost]
        public IActionResult FinalizarDesdeEjercicio()
        {
            int total = Convert.ToInt32(TempData["CantidadEjercicios"]);
            int actual = TempData.ContainsKey("EjerciciosRealizados") ? Convert.ToInt32(TempData["EjerciciosRealizados"]) : 0;

            var resultadosJson = TempData["Resultados"] as string;
            var resultados = string.IsNullOrEmpty(resultadosJson)
                ? new List<RMultiplicacionModel>()
                : JsonSerializer.Deserialize<List<RMultiplicacionModel>>(resultadosJson);

            // Rellenar los ejercicios faltantes con "no respondido"
            for (int i = actual; i < total; i++)
            {
                resultados.Add(new RMultiplicacionModel
                {
                    Multiplicando = 0,
                    Multiplicador = 0,
                    RespuestaUsuario = -1
                });
            }

            TempData["Resultados"] = JsonSerializer.Serialize(resultados);
            TempData["EjerciciosRealizados"] = total;

            return RedirectToAction("ResultadoTablas");
        }

        [HttpGet]
        public IActionResult ResultadoTablas()
        {
            var resultadosJson = TempData["Resultados"] as string;
            var resultados = string.IsNullOrEmpty(resultadosJson)
                ? new List<RMultiplicacionModel>()
                : JsonSerializer.Deserialize<List<RMultiplicacionModel>>(resultadosJson);

            int total = resultados.Count;
            int correctas = resultados.Count(r => r.EsCorrecto);
            int incorrectas = resultados.Count(r => r.RespuestaUsuario != -1 && !r.EsCorrecto);
            int noRespondidas = resultados.Count(r => r.RespuestaUsuario == -1);

            ViewBag.Total = total;
            ViewBag.Correctas = correctas;
            ViewBag.Incorrectas = incorrectas;
            ViewBag.NoRespondidas = noRespondidas;

            return View(resultados);
        }

        [HttpPost]
        public IActionResult RepetirEjercicioTablas()
        {
            var configStr = HttpContext.Session.GetString("ConfTablas");
            if (string.IsNullOrEmpty(configStr))
                return RedirectToAction("TablasForm");

            var config = JsonSerializer.Deserialize<ConfTablasModel>(configStr);


            TempData.Clear();

            TempData["CantidadEjercicios"] = config.CantidadEjercicios;
            TempData["TipoPregunta"] = config.TipoPregunta;
            TempData["ParImpar"] = config.ParImpar;
            TempData["VelocidadPreguntas"] = config.VelocidadPreguntas.Replace(",", ".");
            TempData["TiempoMeditacion"] = config.TiempoMeditacion;
            TempData["DigitosMultiplicacion"] = config.DigitosMultiplicacion;

            return RedirectToAction("ConcentracionTablas");
        }

        [HttpGet]
        public IActionResult LimpiarTablasYDashboard()
        {
            HttpContext.Session.Remove("ConfTablas");
            return RedirectToAction("Dashboard", "Dashboard");
        }



    }
}
