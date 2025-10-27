using AnzanMegaArithmetics.Models;
using AnzanMegaArithmetics.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text.Json;

namespace AnzanMegaArithmetics.Controllers
{
    [Authorize]
    public class SorobanController : Controller
    {
        private readonly IPruebasDBService _pruebasDBService;
        public SorobanController(IPruebasDBService pruebasDBService) 
        {
            this._pruebasDBService = pruebasDBService;
        }

        [HttpGet]
        public IActionResult LecturaSoroban(int? cantidad, long? valMin, long? valMax, string velocidad, int? TiempoMeditacion)
        {
            var userId = HttpContext.Session.GetInt32("Id_Usuario");

            if (userId == null || userId == 0) 
            {
                return RedirectToAction("Inicio", "Inicio");
            }

            var model = new ConfLecturaSorobanModel
            {
                CantidadEjercicios = cantidad ?? 5,
                VMinimo = valMin ?? 0,
                VMaximo = valMax ?? 99,
                VelocidadPreguntas = velocidad ?? "0",
                TiempoMeditacion = TiempoMeditacion ?? 3
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult ConcentracionLS(ConfLecturaSorobanModel config)
        {
            config.CantidadEjercicios = Math.Max(1, config.CantidadEjercicios);
            config.TiempoMeditacion = Math.Max(0, config.TiempoMeditacion);

            TempData["VMinimo"] = config.VMinimo.ToString();
            TempData["VMaximo"] = config.VMaximo.ToString();
            TempData["VelocidadPreguntas"] = config.VelocidadPreguntas;
            TempData["TiempoMeditacion"] = config.TiempoMeditacion;
            TempData["CantidadEjercicios"] = config.CantidadEjercicios;
            TempData["EjerciciosRealizados"] = 0;
            TempData["Resultados"] = JsonSerializer.Serialize(new List<RLecturaSorobanModel>());

            ViewBag.TiempoMeditacion = config.TiempoMeditacion;
            ViewBag.VelocidadPreguntas = config.VelocidadPreguntas;

            return View("ConcentracionLS");
        }

        [HttpGet]
        public IActionResult EjercicioLecturaSB()
        {
            long valMin = long.Parse(TempData["VMinimo"].ToString());
            long valMax = long.Parse(TempData["VMaximo"].ToString());
            float velocidad = float.Parse(TempData["VelocidadPreguntas"].ToString().Replace(",", "."), CultureInfo.InvariantCulture);
            int cantidadEjercicios = Convert.ToInt32(TempData["CantidadEjercicios"]);
            int ejerciciosRealizados = Convert.ToInt32(TempData["EjerciciosRealizados"]);

            var resultadosJson = TempData["Resultados"] as string;
            List<RLecturaSorobanModel> resultados = string.IsNullOrEmpty(resultadosJson)
                ? new List<RLecturaSorobanModel>()
                : JsonSerializer.Deserialize<List<RLecturaSorobanModel>>(resultadosJson);

            if (ejerciciosRealizados >= cantidadEjercicios)
            {
                TempData["Resultados"] = JsonSerializer.Serialize(resultados);
                return RedirectToAction("ResultadoLecturaSoroban");
            }

            if (valMax > long.MaxValue)
            {
                valMax = long.MaxValue;
            }

            long numeroObjetivo = Random.Shared.NextInt64((long)valMin, (long)(valMax + 1));
            string numStr = numeroObjetivo.ToString();
            int columnas = numStr.Length;

            var sorobanCuentas = new List<(int columna, bool esSuperior, bool activa)>();
            for (int i = 0; i < columnas; i++)
            {
                int digito = int.Parse(numStr[i].ToString());
                int columnaSoroban = columnas - 1 - i;
                bool supActiva = digito >= 5;
                sorobanCuentas.Add((columnaSoroban, true, supActiva));

                int infActivas = supActiva ? digito - 5 : digito;
                for (int j = 0; j < 4; j++)
                {
                    bool infActiva = j < infActivas;
                    sorobanCuentas.Add((columnaSoroban, false, infActiva));
                }
            }

            ViewBag.NumeroObjetivo = numeroObjetivo;
            ViewBag.RespuestaCorrecta = numeroObjetivo;
            ViewBag.VelocidadPreguntas = velocidad;
            ViewBag.EjercicioActual = ejerciciosRealizados + 1;
            ViewBag.TotalEjercicios = cantidadEjercicios;
            ViewBag.SorobanCuentas = sorobanCuentas;
            ViewBag.Columnas = columnas;

            TempData["NumeroObjetivo"] = numeroObjetivo.ToString();
            TempData["EjerciciosRealizados"] = ejerciciosRealizados;
            TempData["Resultados"] = JsonSerializer.Serialize(resultados);
            TempData["CantidadEjercicios"] = cantidadEjercicios;
            TempData["VMinimo"] = valMin.ToString();
            TempData["VMaximo"] = valMax.ToString();
            TempData["VelocidadPreguntas"] = velocidad.ToString(CultureInfo.InvariantCulture);
            TempData.Keep();

            return View();
        }

        [HttpPost]
        public IActionResult ResultadoLecturaSoroban(int respuesta, string respondido)
        {
            bool respondio = respondido == "true";
            long numeroCorrecto = Convert.ToInt64(TempData["NumeroObjetivo"]);
            int ejerciciosRealizados = Convert.ToInt32(TempData["EjerciciosRealizados"]);

            var resultadosJson = TempData["Resultados"] as string;
            List<RLecturaSorobanModel> resultados = string.IsNullOrEmpty(resultadosJson)
                ? new List<RLecturaSorobanModel>()
                : JsonSerializer.Deserialize<List<RLecturaSorobanModel>>(resultadosJson);

            resultados.Add(new RLecturaSorobanModel
            {
                RespuestaUsuario = respondio ? respuesta : -1,
                RespuestaCorrecta = numeroCorrecto
            });

            ejerciciosRealizados++;
            TempData["Resultados"] = JsonSerializer.Serialize(resultados);
            TempData["EjerciciosRealizados"] = ejerciciosRealizados;
            TempData["CantidadEjercicios"] = TempData.Peek("CantidadEjercicios");
            TempData["VMinimo"] = TempData.Peek("VMinimo")?.ToString();
            TempData["VMaximo"] = TempData.Peek("VMaximo")?.ToString();
            TempData["VelocidadPreguntas"] = TempData.Peek("VelocidadPreguntas");

            if (ejerciciosRealizados >= Convert.ToInt32(TempData.Peek("CantidadEjercicios")))
            {
                return RedirectToAction("ResultadoLecturaSoroban");
            }

            return RedirectToAction("EjercicioLecturaSB");
        }

        [HttpPost]
        public IActionResult FinalizarLecturaSoroban(int respuesta, string respondido, long respuestaCorrecta)
        {
            var resultadosJson = TempData["Resultados"] as string;
            List<RLecturaSorobanModel> resultados = string.IsNullOrEmpty(resultadosJson)
                ? new List<RLecturaSorobanModel>()
                : JsonSerializer.Deserialize<List<RLecturaSorobanModel>>(resultadosJson);

            bool respondio = respondido == "true";
            resultados.Add(new RLecturaSorobanModel
            {
                RespuestaUsuario = respondio ? respuesta : -1,
                RespuestaCorrecta = respuestaCorrecta
            });

            int ejerciciosRealizados = resultados.Count;
            int cantidadEjercicios = Convert.ToInt32(TempData.Peek("CantidadEjercicios"));

            for (int i = ejerciciosRealizados; i < cantidadEjercicios; i++)
            {
                resultados.Add(new RLecturaSorobanModel
                {
                    RespuestaUsuario = -1,
                    RespuestaCorrecta = 0
                });
            }

            TempData["Resultados"] = JsonSerializer.Serialize(resultados);
            return RedirectToAction("ResultadoLecturaSoroban");
        }

        [HttpGet]
        public IActionResult ResultadoLecturaSoroban()
        {
            var userId = HttpContext.Session.GetInt32("Id_Usuario");

            if (userId == null || userId == 0)
            {
                return RedirectToAction("Inicio", "Inicio");
            }

            var resultados = JsonSerializer.Deserialize<List<RLecturaSorobanModel>>(TempData["Resultados"] as string) ?? new List<RLecturaSorobanModel>();
            int cantidadEjercicios = Convert.ToInt32(TempData.Peek("CantidadEjercicios"));

            int correctos = resultados.Count(r => r.RespuestaUsuario == r.RespuestaCorrecta);
            int incorrectos = resultados.Count(r => r.RespuestaUsuario != -1 && r.RespuestaUsuario != r.RespuestaCorrecta);

            int porcentaje = cantidadEjercicios > 0 ? (correctos * 100) / cantidadEjercicios : 0;
            int xp = porcentaje;

            PruebasDBModel results = new PruebasDBModel
            {
                Id_Usuario = userId.Value,
                Total_Preguntas = cantidadEjercicios,
                Respuestas_Correctas = correctos,
                Fecha = DateTime.Now,
                Tipo_Prueba = "Soroban Lectura",
                ExperienciaAdquirida = xp
            };

            bool InsertarPrueba = _pruebasDBService.GuardarPrueba(results);

            TempData.Keep("Resultados");
            return View();
        }


        //escritura soroban
        [HttpGet]
        public IActionResult EscrituraSoroban(int? cantidad, long? valMin, long? valMax, string velocidad, int? TiempoMeditacion)
        {
            var userId = HttpContext.Session.GetInt32("Id_Usuario");

            if (userId == null || userId == 0)
            {
                return RedirectToAction("Inicio", "Inicio");
            }

            var model = new ConfEscrituraSorobanModel
            {
                CantidadEjercicios = cantidad ?? 5,
                VMinimo = valMin ?? 0,
                VMaximo = valMax ?? 99,
                VelocidadPreguntas = velocidad ?? "0",
                TiempoMeditacion = TiempoMeditacion ?? 3
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult ConcentracionES(ConfEscrituraSorobanModel config)
        {
            config.CantidadEjercicios = Math.Max(1, config.CantidadEjercicios);
            config.TiempoMeditacion = Math.Max(0, config.TiempoMeditacion);

            TempData["VMinimo"] = config.VMinimo.ToString();
            TempData["VMaximo"] = config.VMaximo.ToString();

            TempData["VelocidadPreguntas"] = config.VelocidadPreguntas;
            TempData["TiempoMeditacion"] = config.TiempoMeditacion;
            TempData["CantidadEjercicios"] = config.CantidadEjercicios;
            TempData["EjerciciosRealizados"] = 0;
            TempData["Resultados"] = JsonSerializer.Serialize(new List<REscrituraSorobanModel>());

            ViewBag.TiempoMeditacion = config.TiempoMeditacion;
            ViewBag.VelocidadPreguntas = config.VelocidadPreguntas;

            return View("ConcentracionES");
        }

        [HttpGet]
        public IActionResult EjercicioEscrituraSB()
        {
            long valMin = long.Parse(TempData["VMinimo"].ToString());
            long valMax = long.Parse(TempData["VMaximo"].ToString());

            float velocidad = float.Parse(TempData["VelocidadPreguntas"].ToString().Replace(",", "."), CultureInfo.InvariantCulture);
            int cantidadEjercicios = Convert.ToInt32(TempData["CantidadEjercicios"]);
            int ejerciciosRealizados = Convert.ToInt32(TempData["EjerciciosRealizados"]);

            var resultadosJson = TempData["Resultados"] as string;
            List<REscrituraSorobanModel> resultados = string.IsNullOrEmpty(resultadosJson)
                ? new List<REscrituraSorobanModel>()
                : JsonSerializer.Deserialize<List<REscrituraSorobanModel>>(resultadosJson);

            if (ejerciciosRealizados >= cantidadEjercicios)
            {
                TempData["Resultados"] = JsonSerializer.Serialize(resultados);
                return RedirectToAction("ResultadoEscrituraSoroban");
            }

            if (valMax > long.MaxValue)
            {
                valMax = long.MaxValue;
            }

            // Generar número objetivo
            long numeroObjetivo = Random.Shared.NextInt64((long)valMin, (long)(valMax + 1));

            // Número de columnas del soroban
            int columnas = numeroObjetivo.ToString().Length;

            ViewBag.NumeroObjetivo = numeroObjetivo;
            ViewBag.Columnas = columnas;
            ViewBag.VelocidadPreguntas = velocidad;
            ViewBag.EjercicioActual = ejerciciosRealizados + 1;
            ViewBag.TotalEjercicios = cantidadEjercicios;

            // Guardar en TempData
            TempData["NumeroObjetivo"] = numeroObjetivo.ToString();
            TempData["EjerciciosRealizados"] = ejerciciosRealizados;
            TempData["Resultados"] = JsonSerializer.Serialize(resultados);
            TempData["CantidadEjercicios"] = cantidadEjercicios;

            TempData["VMinimo"] = valMin.ToString();
            TempData["VMaximo"] = valMax.ToString();

            TempData["VelocidadPreguntas"] = velocidad.ToString(CultureInfo.InvariantCulture);

            TempData.Keep();

            return View();
        }

        [HttpPost]
        public IActionResult ResultadoEscrituraSoroban(int respuesta, string respondido)
        {
            bool respondio = respondido == "true";
            long numeroCorrecto = Convert.ToInt64(TempData["NumeroObjetivo"]);
            int ejerciciosRealizados = Convert.ToInt32(TempData["EjerciciosRealizados"]);

            var resultadosJson = TempData["Resultados"] as string;
            List<REscrituraSorobanModel> resultados = string.IsNullOrEmpty(resultadosJson)
                ? new List<REscrituraSorobanModel>()
                : JsonSerializer.Deserialize<List<REscrituraSorobanModel>>(resultadosJson);

            resultados.Add(new REscrituraSorobanModel
            {
                RespuestaUsuario = (respondio ? respuesta : 0),
                RespuestaCorrecta = numeroCorrecto
            });

            ejerciciosRealizados++;

            TempData["Resultados"] = JsonSerializer.Serialize(resultados);
            TempData["EjerciciosRealizados"] = ejerciciosRealizados;

            // Preservar para el siguiente ejercicio
            TempData["CantidadEjercicios"] = TempData.Peek("CantidadEjercicios");

            TempData["VMinimo"] = TempData.Peek("VMinimo")?.ToString();
            TempData["VMaximo"] = TempData.Peek("VMaximo")?.ToString();

            TempData["VelocidadPreguntas"] = TempData.Peek("VelocidadPreguntas");

            if (ejerciciosRealizados >= Convert.ToInt32(TempData.Peek("CantidadEjercicios")))
            {
                return RedirectToAction("ResultadoEscrituraSoroban");
            }

            return RedirectToAction("EjercicioEscrituraSB");
        }

        [HttpPost]
        public IActionResult FinalizarEscrituraSoroban(int respuesta, string respondido)
        {
            var resultadosJson = TempData["Resultados"] as string;
            List<REscrituraSorobanModel> resultados = string.IsNullOrEmpty(resultadosJson)
                ? new List<REscrituraSorobanModel>()
                : JsonSerializer.Deserialize<List<REscrituraSorobanModel>>(resultadosJson);

            bool respondio = respondido == "true";
            long numeroCorrecto = Convert.ToInt64(TempData["NumeroObjetivo"]);

            resultados.Add(new REscrituraSorobanModel
            {
                RespuestaUsuario = respondio ? respuesta : -1,
                RespuestaCorrecta = numeroCorrecto
            });

            int ejerciciosRealizados = resultados.Count;
            int cantidadEjercicios = Convert.ToInt32(TempData.Peek("CantidadEjercicios"));

            for (int i = ejerciciosRealizados; i < cantidadEjercicios; i++)
            {
                resultados.Add(new REscrituraSorobanModel
                {
                    RespuestaUsuario = -1,
                    RespuestaCorrecta = 0
                });
            }

            TempData["Resultados"] = JsonSerializer.Serialize(resultados);
            return RedirectToAction("ResultadoEscrituraSoroban");
        }


        [HttpGet]
        public IActionResult ResultadoEscrituraSoroban()
        {
            var userId = HttpContext.Session.GetInt32("Id_Usuario");

            if (userId == null || userId == 0)
            {
                return RedirectToAction("Inicio", "Inicio");
            }

            var resultados = JsonSerializer.Deserialize<List<REscrituraSorobanModel>>(TempData["Resultados"] as string) ?? new List<REscrituraSorobanModel>();
            int cantidadEjercicios = Convert.ToInt32(TempData.Peek("CantidadEjercicios"));

            int correctos = resultados.Count(r => r.RespuestaUsuario == r.RespuestaCorrecta);
            int incorrectos = resultados.Count(r => r.RespuestaUsuario != -1 && r.RespuestaUsuario != r.RespuestaCorrecta);

            int porcentaje = cantidadEjercicios > 0 ? (correctos * 100) / cantidadEjercicios : 0;
            int xp = porcentaje;

            PruebasDBModel results = new PruebasDBModel
            {
                Id_Usuario = userId.Value,
                Total_Preguntas = cantidadEjercicios,
                Respuestas_Correctas = correctos,
                Fecha = DateTime.Now,
                Tipo_Prueba = "Soroban Escritura",
                ExperienciaAdquirida = xp
            };

            bool InsertarPrueba = _pruebasDBService.GuardarPrueba(results);

            TempData.Keep("Resultados");
            return View();
        }

        public IActionResult RegresarDashboard()
        {
            return RedirectToAction("Dashboard", "Dashboard");
        }

    }
}
