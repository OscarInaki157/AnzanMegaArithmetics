using AnzanMegaArithmetics.Models;
using AnzanMegaArithmetics.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text.Json;

namespace AnzanMegaArithmetics.Controllers
{
    [Authorize]
    public class MultiplicacionController : Controller
    {
        private readonly IPruebasDBService _pruebasDBService;
        public MultiplicacionController(IPruebasDBService pruebasDBService)
        {
            _pruebasDBService = pruebasDBService;
        }

        [HttpGet]
        public IActionResult TablasForm()
        {
            var userId = HttpContext.Session.GetInt32("Id_Usuario");

            if (userId == null || userId == 0) 
            {
                return RedirectToAction("Inicio", "Inicio");
            }

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
            int longitudMultiplicando = int.Parse(tipo);
            int multiplicando = GenerarNumeroAleatorioT(longitudMultiplicando, random);

            // Filtrar multiplicadores válidos
            var digitosPermitidos = digitosStr.Split(',').Select(d => d.Trim()).Where(d => d.Length == 1).ToArray();
            var posiblesMultiplicadores = Enumerable.Range(1, 9)
                .Where(m =>
                    digitosPermitidos.Contains(m.ToString()) &&
                    (parImpar == "ambos" ||
                     (parImpar == "par" && m % 2 == 0) ||
                     (parImpar == "impar" && m % 2 != 0)))
                .ToList();

            if (!posiblesMultiplicadores.Any())
            {
                posiblesMultiplicadores = digitosPermitidos.Select(int.Parse).ToList();
            }

            int multiplicador = posiblesMultiplicadores[random.Next(posiblesMultiplicadores.Count)];

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

        private int GenerarNumeroAleatorioT(int longitud, Random random)
        {
            string num = "";
            for (int i = 0; i < longitud; i++)
            {
                int digito = i == 0 ? random.Next(1, 10) : random.Next(0, 10); // evitar 0 inicial
                num += digito.ToString();
            }
            return int.Parse(num);
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
        public IActionResult EnviarRespuestaTablas(int? respuestaUsuario, double tiempoRespuesta)
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
                RespuestaUsuario = respuestaUsuario ?? -1,
                TiempoRespuesta = tiempoRespuesta
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
        public IActionResult FinalizarDesdeEjercicio(int? respuestaUsuario, double tiempoRespuesta)
        {
            int total = Convert.ToInt32(TempData["CantidadEjercicios"]);
            int actual = TempData.ContainsKey("EjerciciosRealizados") ? Convert.ToInt32(TempData["EjerciciosRealizados"]) : 0;

            var resultadosJson = TempData["Resultados"] as string;
            var resultados = string.IsNullOrEmpty(resultadosJson)
                ? new List<RMultiplicacionModel>()
                : JsonSerializer.Deserialize<List<RMultiplicacionModel>>(resultadosJson);

            // Guardar el ejercicio actual si hay respuesta
            if (respuestaUsuario.HasValue)
            {
                int multiplicando = Convert.ToInt32(TempData["Multiplicando"]);
                int multiplicador = Convert.ToInt32(TempData["Multiplicador"]);

                resultados.Add(new RMultiplicacionModel
                {
                    Multiplicando = multiplicando,
                    Multiplicador = multiplicador,
                    RespuestaUsuario = respuestaUsuario.Value,
                    TiempoRespuesta = tiempoRespuesta
                });
                actual++; // Incrementar el contador
            }

            // Rellenar los ejercicios faltantes con "no respondido"
            for (int i = resultados.Count; i < total; i++)
            {
                resultados.Add(new RMultiplicacionModel
                {
                    Multiplicando = 0,
                    Multiplicador = 0,
                    RespuestaUsuario = -1,
                    TiempoRespuesta = 0
                });
            }

            TempData["Resultados"] = JsonSerializer.Serialize(resultados);
            TempData["EjerciciosRealizados"] = total;

            return RedirectToAction("ResultadoTablas");
        }

        [HttpGet]
        public IActionResult ResultadoTablas()
        {

            var userId = HttpContext.Session.GetInt32("Id_Usuario");

            if (userId == null || userId == 0)
            {
                return RedirectToAction("Inicio", "Inicio");
            }

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

            var resultadosJson = TempData["Resultados"] as string;
            var resultados = string.IsNullOrEmpty(resultadosJson)
                ? new List<RMultiplicacionModel>()
                : JsonSerializer.Deserialize<List<RMultiplicacionModel>>(resultadosJson);

            int total = resultados.Count;
            int correctas = resultados.Count(r => r.EsCorrecto);
            int incorrectas = resultados.Count(r => r.RespuestaUsuario != -1 && !r.EsCorrecto);
            int noRespondidas = resultados.Count(r => r.RespuestaUsuario == -1);

            int porcentaje = total > 0 ? (correctas * 100) / total : 0;
            int xp = porcentaje;

            double tiempoTotalSegundos = resultados.Sum(r => r.TiempoRespuesta);
            TimeSpan tiempoTotal = TimeSpan.FromSeconds(tiempoTotalSegundos);

            ViewBag.Total = total;
            ViewBag.Correctas = correctas;
            ViewBag.Incorrectas = incorrectas;
            ViewBag.NoRespondidas = noRespondidas;

            PruebasDBModel results = new PruebasDBModel
            {
                Id_Usuario = userId.Value,
                Total_Preguntas = total,
                Respuestas_Correctas = correctas,
                Fecha = DateTime.Now,
                Tipo_Prueba = "Tablas de Multiplicar",
                ExperienciaAdquirida = xp,
                Tiempo = tiempoTotal,
                Configuracion = configStr ?? JsonSerializer.Serialize(modelo)
            };

            bool InsertarPrueba = _pruebasDBService.GuardarPrueba(results);

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

        //multiplicacion normal

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
            var userId = HttpContext.Session.GetInt32("Id_Usuario");

            if (userId == null || userId == 0)
            {
                return RedirectToAction("Inicio", "Inicio");
            }

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
                    FormatoPregunta = "Vertical",
                    DireccionRespuesta = "DerechaAIzquierda",
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
        public IActionResult EnviarRespuesta(string respuestaUsuario, string respondido, double tiempoRespuesta)
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
                RespuestaUsuario = respuesta,
                TiempoRespuesta = tiempoRespuesta
            });

            HttpContext.Session.SetString("ResMultiplicacion", System.Text.Json.JsonSerializer.Serialize(resultados));

            if (ejercicioActual >= total)
                return RedirectToAction("ResultadosMultiplicacion");

            HttpContext.Session.SetInt32("EjercicioActual", ejercicioActual + 1);
            return RedirectToAction("EjercicioMultiplicacion");
        }

        [HttpPost]
        public IActionResult FinalizarMultiplicacion(string respuestaUsuario ,double tiempoRespuesta)
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

            if (!string.IsNullOrEmpty(respuestaUsuario) && int.TryParse(respuestaUsuario, out int respuesta))
            {
                int multiplicando = HttpContext.Session.GetInt32("MultiplicandoActual") ?? 0;
                int multiplicador = HttpContext.Session.GetInt32("MultiplicadorActual") ?? 0;

                resultados.Add(new RMultiplicationModel
                {
                    Multiplicando = multiplicando,
                    Multiplicador = multiplicador,
                    RespuestaUsuario = respuesta,
                    TiempoRespuesta = tiempoRespuesta
                });
                ejercicioActual++;
            }


            var rng = new Random();

            for (int i = ejercicioActual; i <= config.CantidadEjercicios; i++)
            {
                int multiplicando = GenerarNumero(rng, config.DigitosMultiplicando);
                int multiplicador = GenerarNumero(rng, config.DigitosMultiplicador);

                resultados.Add(new RMultiplicationModel
                {
                    Multiplicando = multiplicando,
                    Multiplicador = multiplicador,
                    RespuestaUsuario = -1,
                    TiempoRespuesta = 0
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
            var userId = HttpContext.Session.GetInt32("Id_Usuario");

            if (userId == null || userId == 0)
            {
                return RedirectToAction("Inicio", "Inicio");
            }

            var resJson = HttpContext.Session.GetString("ResMultiplicacion");
            if (!string.IsNullOrEmpty(resJson))
            {
                TempData["Resultados"] = resJson;
            }
            var resultados = string.IsNullOrEmpty(resJson)
               ? new List<RMultiplicationModel>()
               : System.Text.Json.JsonSerializer.Deserialize<List<RMultiplicationModel>>(resJson);

            var configStr = HttpContext.Session.GetString("UltimaConfigMultiplicacion");

            int total = resultados.Count;
            int correctas = resultados.Count(r => r.RespuestaUsuario == r.RespuestaCorrecta);
            int incorrectas = resultados.Count(r => r.RespuestaUsuario != -1 && r.RespuestaUsuario != r.RespuestaCorrecta);

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
                Tipo_Prueba = "Multiplicación",
                ExperienciaAdquirida = xp,
                Tiempo = tiempoTotal,
                Configuracion = configStr ?? "No se pudo recuperar la configuración del servidor"
            };

            bool InsertarPrueba = _pruebasDBService.GuardarPrueba(results);

            return View();
        }


        //competencia

        [HttpGet]
        public IActionResult FormCompetenciaMulti()
        {
            var userId = HttpContext.Session.GetInt32("Id_Usuario");

            if (userId == null || userId == 0)
            {
                return RedirectToAction("Inicio", "Inicio");
            }

            HttpContext.Session.Remove("YaCargoMemorizada");
            HttpContext.Session.Remove("EjerciciosMemorizada");

            HttpContext.Session.Remove("EjerciciosCompetencia");
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
            ["2x1"] = TimeSpan.FromMinutes(1),
            ["2x2"] = TimeSpan.FromMinutes(1.25),
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
            if (digitos == 1)
            {
                return rand.Next(3, 10).ToString();
            }

            int min = (int)Math.Pow(10, digitos - 1);
            int max = (int)Math.Pow(10, digitos) - 1;
            return rand.Next(min, max + 1).ToString();
        }

        [HttpGet]
        public IActionResult EjercicioCompetencia()
        {
            var ejerciciosJson = HttpContext.Session.GetString("EjerciciosCompetencia");
            if (!string.IsNullOrEmpty(ejerciciosJson))
            {
                var ejerciciosExistentes = JsonSerializer.Deserialize<List<EjercicioCompetenciaModel>>(ejerciciosJson);

                var configJson = HttpContext.Session.GetString("UltimaConfigCompetenciaMulti");
                var config = JsonSerializer.Deserialize<ConfCompetenciaMultiModel>(configJson);

                var inicioStr = HttpContext.Session.GetString("InicioCompetencia");
                DateTime inicio = string.IsNullOrEmpty(inicioStr)
                    ? DateTime.UtcNow // fallback si no existe
                    : DateTime.Parse(inicioStr);

                var tiempoTotal = TiempoPorTipo[config.TipoPregunta];
                var transcurrido = DateTime.UtcNow - inicio;
                var tiempoRestante = tiempoTotal - transcurrido;
                if (tiempoRestante < TimeSpan.Zero) tiempoRestante = TimeSpan.Zero;

                ViewBag.TiempoLimiteSeg = tiempoRestante.TotalSeconds;
                ViewBag.MostrarContador = config.MostrarContadorTiempo;

                return View(ejerciciosExistentes);
            }

            var configJsonNuevo = HttpContext.Session.GetString("UltimaConfigCompetenciaMulti");

            if (string.IsNullOrEmpty(configJsonNuevo))
                return RedirectToAction("FormCompetenciaMulti");

            var configNuevo = JsonSerializer.Deserialize<ConfCompetenciaMultiModel>(configJsonNuevo);

            var partes = configNuevo.TipoPregunta.Split('x');
            int digitosMultiplicando = int.Parse(partes[0]);
            int digitosMultiplicador = int.Parse(partes[1]);

            var ejercicios = new List<EjercicioCompetenciaModel>();
            var rand = new Random();
            for (int i = 0; i < 10; i++)
            {
                string multiplicando = GenerarNumeroAleatorio(digitosMultiplicando, rand);
                string multiplicador;

                if (configNuevo.TipoPregunta == "2x1" && digitosMultiplicador == 1) 
                {
                    multiplicador = rand.Next(3,10).ToString();
                }
                else
                {
                    multiplicador = GenerarNumeroAleatorio(digitosMultiplicador, rand);
                }

                ejercicios.Add(new EjercicioCompetenciaModel
                {
                    Multiplicando = multiplicando,
                    Multiplicador = multiplicador,
                    FormatoPregunta = configNuevo.FormatoPregunta,
                    DireccionRespuesta = configNuevo.DireccionRespuesta
                });
            }

            HttpContext.Session.SetString("EjerciciosCompetencia", JsonSerializer.Serialize(ejercicios));

            var inicioCompetencia = DateTime.UtcNow;
            HttpContext.Session.SetString("InicioCompetencia", inicioCompetencia.ToString("o"));

            ViewBag.TiempoLimiteSeg = TiempoPorTipo[configNuevo.TipoPregunta].TotalSeconds;
            ViewBag.MostrarContador = configNuevo.MostrarContadorTiempo;

            return View(ejercicios);
        }

        [HttpPost]
        public IActionResult FinalizarCompetencia(List<string> RespuestasUsuario, long TiempoRealMs = 0)
        {
            var ejerciciosJson = HttpContext.Session.GetString("EjerciciosCompetencia");

            if (string.IsNullOrEmpty(ejerciciosJson))
                return RedirectToAction("FormCompetenciaMulti");

            var ejercicios = JsonSerializer.Deserialize<List<EjercicioCompetenciaModel>>(ejerciciosJson);
            var resultados = new List<RCompetenciaMultiModel>();

            TimeSpan tiempoRealUtilizado;

            // USAR EL TIEMPO DEL FRONTEND
            if (TiempoRealMs > 0)
            {
                tiempoRealUtilizado = TimeSpan.FromMilliseconds(TiempoRealMs);
                Console.WriteLine($"Tiempo recibido del frontend: {tiempoRealUtilizado}");
            }
            else
            {
                // Fallback por si acaso
                var configJson = HttpContext.Session.GetString("UltimaConfigCompetenciaMulti");
                var config = JsonSerializer.Deserialize<ConfCompetenciaMultiModel>(configJson);
                tiempoRealUtilizado = TiempoPorTipo[config.TipoPregunta];
                Console.WriteLine($"Usando tiempo límite como fallback: {tiempoRealUtilizado}");
            }

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
                    TiempoRespuesta = TimeSpan.Zero
                });
            }

            HttpContext.Session.Remove("InicioCompetencia");

            TimeSpan tiempoLimpio = new TimeSpan(tiempoRealUtilizado.Days, tiempoRealUtilizado.Hours, tiempoRealUtilizado.Minutes, tiempoRealUtilizado.Seconds);

            TempData["TiempoRealCompetencia"] = tiempoLimpio.ToString();
            TempData["Resultados"] = JsonSerializer.Serialize(resultados);

            return RedirectToAction("ResultadoCompetencia");
        }

        [HttpGet]
        public IActionResult ResultadoCompetencia()
        {
            var userId = HttpContext.Session.GetInt32("Id_Usuario");

            if (userId == null || userId == 0)
            {
                return RedirectToAction("Inicio", "Inicio");
            }

            var resultadosJson = TempData["Resultados"] as string;
            var resultados = string.IsNullOrEmpty(resultadosJson)
                ? new List<RCompetenciaMultiModel>()
                : JsonSerializer.Deserialize<List<RCompetenciaMultiModel>>(resultadosJson);

            // Obtener configuración
            var configJson = HttpContext.Session.GetString("UltimaConfigCompetenciaMulti");
            var config = !string.IsNullOrEmpty(configJson)
                ? JsonSerializer.Deserialize<ConfCompetenciaMultiModel>(configJson)
                : new ConfCompetenciaMultiModel();

            // Calcular estadísticas
            int total = resultados.Count;
            int correctas = resultados.Count(r => r.RespuestaUsuario == r.RespuestaCorrecta);
            int incorrectas = resultados.Count(r => r.RespuestaUsuario != -1 && r.RespuestaUsuario != r.RespuestaCorrecta);

            int porcentaje = total > 0 ? (correctas * 100) / total : 0;
            int xp = porcentaje;

            // Obtener tiempo real utilizado
            var tiempoRealStr = TempData["TiempoRealCompetencia"] as string;
            TimeSpan tiempoReal = TimeSpan.Zero;


            if (!string.IsNullOrEmpty(tiempoRealStr) && TimeSpan.TryParse(tiempoRealStr, out TimeSpan parsedTime))
            {
                tiempoReal = parsedTime;
            }
            else
            {
                tiempoReal = TiempoPorTipo[config.TipoPregunta];
            }

            ViewBag.TiempoUtilizado = tiempoReal;
            ViewBag.TiempoFormateado = FormatearTiempo(tiempoReal);

            PruebasDBModel results = new PruebasDBModel
            {
                Id_Usuario = userId.Value,
                Total_Preguntas = total,
                Respuestas_Correctas = correctas,
                Fecha = DateTime.Now,
                Tipo_Prueba = $"Competencia Multiplicación - {config.TipoPregunta}",
                ExperienciaAdquirida = xp,
                Tiempo = tiempoReal,
                Configuracion = configJson ?? "No se pudo recuperar la configuración del servidor"
            };

            bool insertarPrueba = _pruebasDBService.GuardarPrueba(results);

            // Limpiar sesión
            HttpContext.Session.Remove("TiempoRealCompetencia");
            HttpContext.Session.Remove("EjerciciosCompetencia");

            return View(resultados);
        }

        private string FormatearTiempo(TimeSpan tiempo)
        {
            if (tiempo.TotalHours >= 1)
            {
                return $"{(int)tiempo.TotalHours}:{tiempo.Minutes:00}:{tiempo.Seconds:00}";
            }
            else if (tiempo.TotalMinutes >= 1)
            {
                return $"{tiempo.Minutes}:{tiempo.Seconds:00}";
            }
            else
            {
                return $"{tiempo.Seconds} segundos";
            }
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

            HttpContext.Session.Remove("EjerciciosCompetencia");

            var tiempo = config.TiempoMeditacion >= 0 ? config.TiempoMeditacion : 3;
            ViewBag.TiempoMeditacion = tiempo;

            return View("ConcentracionCompetencia", config);
        }

        //3ra conf competencia
        [HttpPost]
        public IActionResult ConcentracionMemorizada(ConfCompetenciaMultiModel config)
        {
            var jsonConfig = JsonSerializer.Serialize(config);
            HttpContext.Session.SetString("UltimaConfigMemorizada", jsonConfig);

            ViewBag.TiempoMeditacion = config.TiempoMeditacion;

            return View(config);
        }

        private static readonly Dictionary<string, TimeSpan> TiempoPorTipoTercera = new()
        {
            ["2x2"] = TimeSpan.FromMinutes(1.25).Add(TimeSpan.FromSeconds(30)),
            ["3x3"] = TimeSpan.FromMinutes(2).Add(TimeSpan.FromSeconds(30)),
            ["4x4"] = TimeSpan.FromMinutes(3.75).Add(TimeSpan.FromSeconds(30))
        };


        [HttpGet]
        public IActionResult EjercicioMemorizado()
        {
            if (HttpContext.Session.GetString("YaCargoMemorizada") == "true")
            {
                var json = HttpContext.Session.GetString("EjerciciosMemorizada");

                if (!string.IsNullOrEmpty(json))
                {
                    var ejerciciosCargados = JsonSerializer.Deserialize<List<EjercicioCompetenciaModel>>(json);

                    var resultados = ejerciciosCargados.Select(ej => new RCompetenciaMultiModel
                    {
                        OperacionTexto = $"{ej.Multiplicando} × {ej.Multiplicador}",
                        RespuestaCorrecta = long.Parse(ej.Multiplicando) * long.Parse(ej.Multiplicador),
                        RespuestaUsuario = -1,
                        Respondido = false
                    }).ToList();

                    HttpContext.Session.SetString("ResultadosMemorizada", JsonSerializer.Serialize(resultados));
                }

                return RedirectToAction("ResultadoMemorizada");
            }

            // Marca que ya cargó
            HttpContext.Session.SetString("YaCargoMemorizada", "true");

            // Si ya existen ejercicios, los reutilizamos
            var ejerciciosExistentesJson = HttpContext.Session.GetString("EjerciciosMemorizada");
            if (!string.IsNullOrEmpty(ejerciciosExistentesJson))
            {
                var ejerciciosExistentes = JsonSerializer.Deserialize<List<EjercicioCompetenciaModel>>(ejerciciosExistentesJson);
                var configJson = HttpContext.Session.GetString("UltimaConfigMemorizada");
                var config = JsonSerializer.Deserialize<ConfCompetenciaMultiModel>(configJson);

                ViewBag.MostrarContador = config.MostrarContadorTiempo;
                ViewBag.TiempoMemoriaSeg = TiempoPorTipoTercera[config.TipoPregunta].TotalSeconds;

                return View(ejerciciosExistentes);
            }

            // Si no hay ejercicios, generarlos
            var configNuevoJson = HttpContext.Session.GetString("UltimaConfigMemorizada");
            if (string.IsNullOrEmpty(configNuevoJson))
                return RedirectToAction("FormCompetenciaMulti", "Multiplicacion");

            var configNuevo = JsonSerializer.Deserialize<ConfCompetenciaMultiModel>(configNuevoJson);
            var partes = configNuevo.TipoPregunta.Split('x');
            int digitosMultiplicando = int.Parse(partes[0]);
            int digitosMultiplicador = int.Parse(partes[1]);

            var ejercicios = new List<EjercicioCompetenciaModel>();
            var rand = new Random();
            for (int i = 0; i < 10; i++)
            {
                ejercicios.Add(new EjercicioCompetenciaModel
                {
                    Multiplicando = GenerarNumeroAleatorio(digitosMultiplicando, rand),
                    Multiplicador = GenerarNumeroAleatorio(digitosMultiplicador, rand),
                    FormatoPregunta = configNuevo.FormatoPregunta,
                    DireccionRespuesta = configNuevo.DireccionRespuesta
                });
            }

            HttpContext.Session.SetString("EjerciciosMemorizada", JsonSerializer.Serialize(ejercicios));
            ViewBag.MostrarContador = configNuevo.MostrarContadorTiempo;
            ViewBag.TiempoMemoriaSeg = TiempoPorTipoTercera[configNuevo.TipoPregunta].TotalSeconds;

            return View(ejercicios);
        }



        [HttpPost]
        public IActionResult FinalizarMemorizada(List<string> RespuestasUsuario, long TiempoRealMs)
        {
            var ejerciciosJson = HttpContext.Session.GetString("EjerciciosMemorizada");
            if (string.IsNullOrEmpty(ejerciciosJson))
                return RedirectToAction("FormCompetenciaMulti", "Multiplicacion");

            var ejercicios = JsonSerializer.Deserialize<List<EjercicioCompetenciaModel>>(ejerciciosJson);
            var resultados = new List<RCompetenciaMultiModel>();

            TimeSpan tiempoRealUtilizado;

    
            if (TiempoRealMs > 0)
            {
                tiempoRealUtilizado = TimeSpan.FromMilliseconds(TiempoRealMs);
                Console.WriteLine($"Tiempo recibido del frontend (Memorizada): {tiempoRealUtilizado}");
            }
            else
            {
                
                var configJson = HttpContext.Session.GetString("UltimaConfigMemorizada");
                var config = JsonSerializer.Deserialize<ConfCompetenciaMultiModel>(configJson);
                tiempoRealUtilizado = TiempoPorTipoTercera[config.TipoPregunta].Add(TimeSpan.FromMinutes(5));
                Console.WriteLine($"Usando tiempo estimado como fallback (Memorizada): {tiempoRealUtilizado}");
            }

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
                    EsCorrecto = esCorrecto
                });
            }

            HttpContext.Session.Remove("YaCargoMemorizada");
            TempData["TiempoRealMemorizada"] = tiempoRealUtilizado.ToString();
            HttpContext.Session.SetString("ResultadosMemorizada", JsonSerializer.Serialize(resultados));
            return RedirectToAction("ResultadoMemorizada");
        }

        [HttpGet]
        public IActionResult ResultadoMemorizada()
        {
            var userId = HttpContext.Session.GetInt32("Id_Usuario");

            if (userId == null || userId == 0)
            {
                return RedirectToAction("Inicio", "Inicio");
            }


            var resultadosJson = HttpContext.Session.GetString("ResultadosMemorizada");
            if (string.IsNullOrEmpty(resultadosJson))
                return RedirectToAction("Dashboard", "Dashboard");

            var resultados = JsonSerializer.Deserialize<List<RCompetenciaMultiModel>>(resultadosJson);

            // Obtener configuración
            var configJson = HttpContext.Session.GetString("UltimaConfigMemorizada");
            var config = !string.IsNullOrEmpty(configJson)
                ? JsonSerializer.Deserialize<ConfCompetenciaMultiModel>(configJson)
                : new ConfCompetenciaMultiModel();

            // Calcular estadísticas
            int total = resultados.Count;
            int correctas = resultados.Count(r => r.RespuestaUsuario == r.RespuestaCorrecta);
            int incorrectas = resultados.Count(r => r.RespuestaUsuario != -1 && r.RespuestaUsuario != r.RespuestaCorrecta);
            int noRespondidas = resultados.Count(r => r.RespuestaUsuario == -1);

            int porcentaje = total > 0 ? (correctas * 100) / total : 0;
            int xp = porcentaje;

            TimeSpan tiempoReal;
            var tiempoRealStr = TempData["TiempoRealMemorizada"] as string;

            if (!string.IsNullOrEmpty(tiempoRealStr) && TimeSpan.TryParse(tiempoRealStr, out TimeSpan parsedTime))
            {
                tiempoReal = parsedTime;
            }
            else
            {
                // Fallback al tiempo límite
                tiempoReal = TiempoPorTipoTercera.ContainsKey(config.TipoPregunta)
                    ? TiempoPorTipoTercera[config.TipoPregunta]
                    : TimeSpan.FromMinutes(5);
            }

            ViewBag.TiempoUtilizado = tiempoReal;
            ViewBag.TiempoFormateado = FormatearTiempo(tiempoReal);

            // Guardar en BD
            PruebasDBModel results = new PruebasDBModel
            {
                Id_Usuario = userId.Value,
                Total_Preguntas = total,
                Respuestas_Correctas = correctas,
                Fecha = DateTime.Now,
                Tipo_Prueba = $"Competencia Memorizada - {config.TipoPregunta}",
                ExperienciaAdquirida = xp,
                Tiempo = tiempoReal,
                Configuracion = configJson ?? "No se pudo recuperar la configuración del servidor"
            };

            bool insertarPrueba = _pruebasDBService.GuardarPrueba(results);


            HttpContext.Session.Remove("YaCargoMemorizada");
            HttpContext.Session.Remove("ResultadosMemorizada");
            HttpContext.Session.Remove("EjerciciosMemorizada");
            HttpContext.Session.Remove("UltimaConfigMemorizada");

            return View(resultados);
        }

        [HttpPost]
        public IActionResult RepetirMemorizada()
        {
            var configJson = HttpContext.Session.GetString("UltimaConfigMemorizada");

            if (string.IsNullOrEmpty(configJson))
                return RedirectToAction("FormCompetenciaMulti", "Multiplicacion");

            var config = JsonSerializer.Deserialize<ConfCompetenciaMultiModel>(configJson);

            return View("ConcentracionMemorizada", config);
        }


        [HttpGet]
        public IActionResult LimpiarMemorizadaYDashboard()
        {
            HttpContext.Session.Remove("YaCargoMemorizada");
            HttpContext.Session.Remove("ResultadosMemorizada");
            HttpContext.Session.Remove("EjerciciosMemorizada");
            HttpContext.Session.Remove("UltimaConfigMemorizada");

            return RedirectToAction("Dashboard", "Dashboard");
        }



    }
}
