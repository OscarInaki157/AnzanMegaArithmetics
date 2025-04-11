using AnzanMegaArithmetics.Models;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text.Json;

namespace AnzanMegaArithmetics.Controllers
{
    public class SorobanController : Controller
    {
        public IActionResult LecturaSoroban(int ? cantidad, int ? valMin, int ? valMax, string velocidad)
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

            TempData["VMinimo"] = config.VMinimo;
            TempData["VMaximo"] = config.VMaximo; 
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
            int valMin = Convert.ToInt32(TempData["VMinimo"]);
            int valMax = Convert.ToInt32(TempData["VMaximo"]);
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

            // Generar número objetivo
            Random rnd = new Random();
            int numeroObjetivo = rnd.Next(valMin, valMax + 1);

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
            TempData["NumeroObjetivo"] = numeroObjetivo;
            TempData["EjerciciosRealizados"] = ejerciciosRealizados;
            TempData["Resultados"] = JsonSerializer.Serialize(resultados);
            TempData["CantidadEjercicios"] = cantidadEjercicios;
            TempData["VMinimo"] = valMin;
            TempData["VMaximo"] = valMax;
            TempData["VelocidadPreguntas"] = velocidad.ToString(CultureInfo.InvariantCulture);

            TempData.Keep();

            return View();
        }


        [HttpPost]
        public IActionResult ResultadoLecturaSoroban(int respuesta, string respondido)
        {
            bool respondio = respondido == "true";
            int numeroCorrecto = Convert.ToInt32(TempData["NumeroObjetivo"]);
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
            TempData["VMinimo"] = TempData.Peek("VMinimo");
            TempData["VMaximo"] = TempData.Peek("VMaximo");
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
    }
}
