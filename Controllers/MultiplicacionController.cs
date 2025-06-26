using AnzanMegaArithmetics.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
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

        [HttpGet]
        public IActionResult LimpiarMultiplicacionYDashboard()
        {
            HttpContext.Session.Remove("ConfMultiplicacion");
            HttpContext.Session.Remove("EjercicioActual");
            HttpContext.Session.Remove("ResMultiplicacion");
            HttpContext.Session.Remove("UltimaConfigMultiplicacion");
            HttpContext.Session.Remove("InicioEjercicio");

            return RedirectToAction("Dashboard", "Dashboard");
        }


        [HttpGet]
        public IActionResult MultiplicacionForm()
        {
            var configJson = HttpContext.Session.GetString("UltimaConfigMultiplicacion");
            ConfMultiModel modelo;

            if (!string.IsNullOrEmpty(configJson))
            {
                modelo = System.Text.Json.JsonSerializer.Deserialize<ConfMultiModel>(configJson);
            }
            else
            {
                modelo = new ConfMultiModel
                {
                    CantidadEjercicios = 5,
                    FormatoPregunta = "Horizontal",
                    DireccionRespuesta = "IzquierdaADerecha",
                    VelocidadPreguntas = "0.0",
                    DigitosMultiplicando = "2",
                    DigitosMultiplicador = "2",
                    TiempoMeditacion = 3
                };
            }

            return View(modelo);
        }


        [HttpPost]
        public IActionResult RepetirMultiplicacion()
        {
            var configJson = HttpContext.Session.GetString("UltimaConfigMultiplicacion");

            if (!string.IsNullOrEmpty(configJson))
            {
                var config = System.Text.Json.JsonSerializer.Deserialize<ConfMultiModel>(configJson);

                // Reiniciar sesión
                HttpContext.Session.SetString("ConfMultiplicacion", configJson);
                HttpContext.Session.SetInt32("EjercicioActual", 1);
                HttpContext.Session.SetString("ResMultiplicacion", System.Text.Json.JsonSerializer.Serialize(new List<RMultiplicationModel>()));

                ViewBag.TiempoMeditacion = config.TiempoMeditacion;
                ViewBag.VelocidadPreguntas = float.Parse(config.VelocidadPreguntas.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);

                return View("ConcentracionMultiplicacion");
            }

            return RedirectToAction("MultiplicacionForm");
        }



        [HttpPost]
        public IActionResult ConcentracionMultiplicacion(ConfMultiModel config)
        {
            HttpContext.Session.SetString("ConfMultiplicacion", System.Text.Json.JsonSerializer.Serialize(config));
            HttpContext.Session.SetInt32("EjercicioActual", 1);
            HttpContext.Session.SetString("ResMultiplicacion", System.Text.Json.JsonSerializer.Serialize(new List<RMultiplicationModel>()));
            HttpContext.Session.SetString("UltimaConfigMultiplicacion", System.Text.Json.JsonSerializer.Serialize(config));

            ViewBag.TiempoMeditacion = config.TiempoMeditacion;
            ViewBag.VelocidadPreguntas = float.Parse(config.VelocidadPreguntas.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);

            return View();
        }


        [HttpGet]
        public IActionResult EjercicioMultiplicacion()
        {
            var configJson = HttpContext.Session.GetString("ConfMultiplicacion");
            var ejercicioActual = HttpContext.Session.GetInt32("EjercicioActual") ?? 1;
            if (string.IsNullOrEmpty(configJson)) return RedirectToAction("MultiplicacionForm");

            var config = System.Text.Json.JsonSerializer.Deserialize<ConfMultiModel>(configJson);

            // Generar multiplicando y multiplicador basados en los dígitos configurados
            var rng = new Random();
            int multiplicando = GenerarNumero(rng, config.DigitosMultiplicando);
            int multiplicador = GenerarNumero(rng, config.DigitosMultiplicador);

            ViewBag.Multiplicando = multiplicando;
            ViewBag.Multiplicador = multiplicador;
            ViewBag.VelocidadPreguntas = float.Parse(config.VelocidadPreguntas.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
            ViewBag.FormatoPregunta = config.FormatoPregunta;
            ViewBag.DireccionRespuesta = config.DireccionRespuesta;
            ViewBag.EjercicioActual = ejercicioActual;
            ViewBag.TotalEjercicios = config.CantidadEjercicios;

            HttpContext.Session.SetInt32("MultiplicandoActual", multiplicando);
            HttpContext.Session.SetInt32("MultiplicadorActual", multiplicador);


            return View();
        }

        [HttpPost]
        public IActionResult EnviarRespuesta(string respuestaUsuario, string respondido)
        {
            var ejercicioActual = HttpContext.Session.GetInt32("EjercicioActual") ?? 1;
            var total = System.Text.Json.JsonSerializer.Deserialize<ConfMultiModel>(
                HttpContext.Session.GetString("ConfMultiplicacion")
            ).CantidadEjercicios;

            var resJson = HttpContext.Session.GetString("ResMultiplicacion");
            var resultados = string.IsNullOrEmpty(resJson)
                ? new List<RMultiplicationModel>()
                : System.Text.Json.JsonSerializer.Deserialize<List<RMultiplicationModel>>(resJson);

            int multiplicando = HttpContext.Session.GetInt32("MultiplicandoActual") ?? 0;
            int multiplicador = HttpContext.Session.GetInt32("MultiplicadorActual") ?? 0;
            int respuesta = string.IsNullOrWhiteSpace(respuestaUsuario) ? -1 : int.Parse(respuestaUsuario);

            resultados.Add(new RMultiplicationModel
            {
                Multiplicando = multiplicando,
                Multiplicador = multiplicador,
                RespuestaUsuario = respuesta
            });

            HttpContext.Session.SetString("ResMultiplicacion", System.Text.Json.JsonSerializer.Serialize(resultados));

            if (ejercicioActual >= total)
                return RedirectToAction("ResultadosMultiplicacion");

            HttpContext.Session.SetInt32("EjercicioActual", ejercicioActual + 1);
            return RedirectToAction("EjercicioMultiplicacion");
        }


        [HttpPost]
        public IActionResult FinalizarMultiplicacion()
        {
            var configJson = HttpContext.Session.GetString("ConfMultiplicacion");
            var resJson = HttpContext.Session.GetString("ResMultiplicacion");

            if (string.IsNullOrEmpty(configJson))
                return RedirectToAction("MultiplicacionForm");

            var config = System.Text.Json.JsonSerializer.Deserialize<ConfMultiModel>(configJson);
            var resultados = string.IsNullOrEmpty(resJson)
                ? new List<RMultiplicationModel>()
                : System.Text.Json.JsonSerializer.Deserialize<List<RMultiplicationModel>>(resJson);

            var ejercicioActual = HttpContext.Session.GetInt32("EjercicioActual") ?? 1;

            var rng = new Random();

            for (int i = ejercicioActual; i <= config.CantidadEjercicios; i++)
            {
                int multiplicando = GenerarNumero(rng, config.DigitosMultiplicando);
                int multiplicador = GenerarNumero(rng, config.DigitosMultiplicador);
                int correcta = multiplicando * multiplicador;

                resultados.Add(new RMultiplicationModel
                {
                    Multiplicando = multiplicando,
                    Multiplicador = multiplicador,
                    RespuestaUsuario = -1
                });
            }

            HttpContext.Session.SetString("ResMultiplicacion", System.Text.Json.JsonSerializer.Serialize(resultados));
            return RedirectToAction("ResultadosMultiplicacion");
        }


        private int GenerarNumero(Random rng, string digitosPermitidos)
        {
            var digitos = digitosPermitidos.Split(',').Select(int.Parse).ToList();
            int d = digitos[rng.Next(digitos.Count)];
            int min = (int)Math.Pow(10, d - 1);
            int max = (int)Math.Pow(10, d) - 1;
            return rng.Next(min, max + 1);
        }

        [HttpGet]
        public IActionResult ResultadosMultiplicacion()
        {
            var resJson = HttpContext.Session.GetString("ResMultiplicacion");
            if (!string.IsNullOrEmpty(resJson))
            {
                TempData["Resultados"] = resJson;
            }

            return View();
        }

        [HttpGet]
        public IActionResult FormCompetenciaMulti()
        {
            var json = HttpContext.Session.GetString("UltimaConfigCompetenciaMulti");

            if (!string.IsNullOrEmpty(json))
            {
                var model = JsonSerializer.Deserialize<ConfCompetenciaMultiModel>(json);
                return View(model);
            }

            return View(new ConfCompetenciaMultiModel
            {
                TipoPregunta = "3x3",
                DireccionRespuesta = "IzquierdaADerecha",
                FormatoPregunta = "Vertical",
                MostrarContadorTiempo = true,
                TiempoMeditacion = 3
            });
        }


        [HttpPost]
        public IActionResult ConcentracionCompetencia(ConfCompetenciaMultiModel config)
        {
            
            var jsonConfig = JsonSerializer.Serialize(config);
            HttpContext.Session.SetString("UltimaConfigCompetenciaMulti", jsonConfig);

            ViewBag.TiempoMeditacion = config.TiempoMeditacion;

            return View(config);
        }

        private static readonly Dictionary<string, TimeSpan> TiempoPorTipo = new()
        {
            ["3x3"] = TimeSpan.FromMinutes(2),
            ["4x4"] = TimeSpan.FromMinutes(3.75),
            ["5x5"] = TimeSpan.FromMinutes(6),
            ["6x6"] = TimeSpan.FromMinutes(8.5),
            ["7x7"] = TimeSpan.FromMinutes(11.5),
            ["8x8"] = TimeSpan.FromMinutes(15),
            ["9x9"] = TimeSpan.FromMinutes(19),
            ["10x10"] = TimeSpan.FromMinutes(23.5)
        };

        private static string GenerarNumeroAleatorio(int digitos, Random rand)
        {
            int min = (int)Math.Pow(10, digitos - 1);
            int max = (int)Math.Pow(10, digitos) - 1;
            return rand.Next(min, max + 1).ToString();
        }

        [HttpGet]
        public IActionResult EjercicioCompetencia()
        {
            var jsonConfig = HttpContext.Session.GetString("UltimaConfigCompetenciaMulti");

            if (string.IsNullOrEmpty(jsonConfig))
                return RedirectToAction("FormCompetenciaMulti");

            var config = JsonSerializer.Deserialize<ConfCompetenciaMultiModel>(jsonConfig);

            var partes = config.TipoPregunta.Split('x');
            int digitosMultiplicando = int.Parse(partes[0]);
            int digitosMultiplicador = int.Parse(partes[1]);

            var ejercicios = new List<EjercicioCompetenciaModel>();
            var rand = new Random();
            for (int i = 0; i < 10; i++)
            {
                var multiplicando = GenerarNumeroAleatorio(digitosMultiplicando, rand);
                var multiplicador = GenerarNumeroAleatorio(digitosMultiplicador, rand);

                ejercicios.Add(new EjercicioCompetenciaModel
                {
                    Multiplicando = multiplicando,
                    Multiplicador = multiplicador,
                    FormatoPregunta = config.FormatoPregunta,
                    DireccionRespuesta = config.DireccionRespuesta
                });
            }

            HttpContext.Session.SetString("EjerciciosCompetencia", JsonSerializer.Serialize(ejercicios));

            ViewBag.TiempoLimiteSeg = TiempoPorTipo[config.TipoPregunta].TotalSeconds;
            ViewBag.MostrarContador = config.MostrarContadorTiempo;

            return View(ejercicios);
        }


        [HttpPost]
        public IActionResult FinalizarCompetencia(List<string> RespuestasUsuario)
        {
            var ejerciciosJson = HttpContext.Session.GetString("EjerciciosCompetencia");

            if (string.IsNullOrEmpty(ejerciciosJson))
                return RedirectToAction("FormCompetenciaMulti");

            var ejercicios = JsonSerializer.Deserialize<List<EjercicioCompetenciaModel>>(ejerciciosJson);
            var resultados = new List<RCompetenciaMultiModel>();

            for (int i = 0; i < ejercicios.Count; i++)
            {
                var ej = ejercicios[i];
                ej.RespuestaUsuario = i < RespuestasUsuario.Count ? RespuestasUsuario[i] : null;

                var resultadoEsperado = long.Parse(ej.Multiplicando) * long.Parse(ej.Multiplicador);
                bool respondido = !string.IsNullOrWhiteSpace(ej.RespuestaUsuario);
                bool esCorrecto = false;
                long respuestaUsuario = -1;

                if (respondido && long.TryParse(ej.RespuestaUsuario, out long parsed))
                {
                    respuestaUsuario = parsed;
                    esCorrecto = parsed == resultadoEsperado;
                }

                resultados.Add(new RCompetenciaMultiModel
                {
                    OperacionTexto = $"{ej.Multiplicando} × {ej.Multiplicador}",
                    RespuestaCorrecta = resultadoEsperado,
                    RespuestaUsuario = respuestaUsuario,
                    Respondido = respondido,
                    EsCorrecto = esCorrecto,
                    TiempoRespuesta = ej.TiempoRespuesta
                });
            }

            TempData["Resultados"] = JsonSerializer.Serialize(resultados);
            return RedirectToAction("ResultadoCompetencia");
        }


        public IActionResult ResultadoCompetencia()
        {
            var resultadosJson = TempData["Resultados"] as string;
            var resultados = string.IsNullOrEmpty(resultadosJson)
                ? new List<RCompetenciaMultiModel>()
                : JsonSerializer.Deserialize<List<RCompetenciaMultiModel>>(resultadosJson);

            return View(resultados);
        }

        [HttpGet]
        public IActionResult LimpiarCompetenciaYDashboard()
        {
            HttpContext.Session.Remove("UltimaConfigCompetenciaMulti");
            HttpContext.Session.Remove("EjerciciosCompetencia");

            return RedirectToAction("Dashboard", "Dashboard");
        }

        [HttpPost]
        public IActionResult RepetirCompetencia()
        {
            var json = HttpContext.Session.GetString("UltimaConfigCompetenciaMulti");

            if (string.IsNullOrEmpty(json))
                return RedirectToAction("FormCompetenciaMulti");

            var config = JsonSerializer.Deserialize<ConfCompetenciaMultiModel>(json);

            var tiempo = config.TiempoMeditacion >= 0 ? config.TiempoMeditacion : 3;
            ViewBag.TiempoMeditacion = tiempo;

            return View("ConcentracionCompetencia", config);
        }


    }
}
