using AnzanMegaArithmetics.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text.Json;

namespace AnzanMegaArithmetics.Controllers
{
    [Authorize]
    public class SorobanController : Controller
    {
        public IActionResult LecturaSoroban(int ? cantidad, long ? valMin, long ? valMax, string velocidad)
        {
            var model = new ConfLecturaSorobanModel 
            {
                CantidadEjercicios = cantidad ?? 5,
                VMinimo = valMin ?? 0,
                VMaximo = valMax ?? 99,
                VelocidadPreguntas = velocidad ?? "0",
                TiempoMeditacion = 3
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

            // Generar número objetivo
            long numeroObjetivo = Random.Shared.NextInt64((long)valMin, (long)(valMax + 1));

            // Preparar estructura del Soroban
            string numStr = numeroObjetivo.ToString();
            int columnas = numStr.Length;

            var sorobanCuentas = new List<(int columna, bool esSuperior, bool activa)>();

            for (int i = 0; i < columnas; i++)
            {
              
                int digito = int.Parse(numStr[i].ToString());

                int columnaSoroban = columnas - 1 - i;

                // Cuenta superior (vale 5)
                bool supActiva = digito >= 5;
                sorobanCuentas.Add((columnaSoroban, true, supActiva));

                // Cuentas inferiores (4 de valor 1)
                int infActivas = supActiva ? digito - 5 : digito;
                for (int j = 0; j < 4; j++)
                {
                    bool infActiva = j < infActivas;
                    sorobanCuentas.Add((columnaSoroban, false, infActiva));
                }
            }


            // ViewBags para la vista
            ViewBag.NumeroObjetivo = numeroObjetivo;
            ViewBag.RespuestaCorrecta = numeroObjetivo;
            ViewBag.VelocidadPreguntas = velocidad;
            ViewBag.EjercicioActual = ejerciciosRealizados + 1;
            ViewBag.TotalEjercicios = cantidadEjercicios;
            ViewBag.SorobanCuentas = sorobanCuentas;
            ViewBag.Columnas = columnas;

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
                RespuestaUsuario = (respondio ? respuesta : 0),
                RespuestaCorrecta = numeroCorrecto
            });

            ejerciciosRealizados++;

            TempData["Resultados"] = JsonSerializer.Serialize(resultados);
            TempData["EjerciciosRealizados"] = ejerciciosRealizados;

            TempData["CantidadEjercicios"] = TempData.Peek("CantidadEjercicios");

            TempData["VMinimo"] = TempData.Peek("VMinimo")?.ToString();
            TempData["VMaximo"] = TempData.Peek("VMaximo")?.ToString();


            TempData["VelocidadPreguntas"] = TempData.Peek("VelocidadPreguntas");

            return RedirectToAction("EjercicioLecturaSB");
        }

        [HttpPost]
        public IActionResult FinalizarLecturaSoroban()
        {
            var resultadosJson = TempData["Resultados"] as string;
            List<RLecturaSorobanModel> resultados = string.IsNullOrEmpty(resultadosJson)
                ? new List<RLecturaSorobanModel>()
                : JsonSerializer.Deserialize<List<RLecturaSorobanModel>>(resultadosJson);

            TempData["Resultados"] = JsonSerializer.Serialize(resultados);
            return RedirectToAction("ResultadoLecturaSoroban");
        }

        [HttpGet]
        public IActionResult ResultadoLecturaSoroban()
        {
            TempData.Keep("Resultados");
            return View();
        }

        //Métodos de soroban Escritura

        [HttpGet]
        public IActionResult EscrituraSoroban(int? cantidad, long? valMin, long? valMax, string velocidad)
        {
            var model = new ConfEscrituraSorobanModel
            {
                CantidadEjercicios = cantidad ?? 5,
                VMinimo = valMin ?? 0,
                VMaximo = valMax ?? 99,
                VelocidadPreguntas = velocidad ?? "0",
                TiempoMeditacion = 3
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
        public IActionResult FinalizarEscrituraSoroban()
        {
            var resultadosJson = TempData["Resultados"] as string;
            List<REscrituraSorobanModel> resultados = string.IsNullOrEmpty(resultadosJson)
                ? new List<REscrituraSorobanModel>()
                : JsonSerializer.Deserialize<List<REscrituraSorobanModel>>(resultadosJson);

            TempData["Resultados"] = JsonSerializer.Serialize(resultados);
            return RedirectToAction("ResultadoEscrituraSoroban");
        }

        [HttpGet]
        public IActionResult ResultadoEscrituraSoroban()
        {
            TempData.Keep("Resultados");
            return View();
        }

    }
}
