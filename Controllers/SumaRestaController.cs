using AnzanMegaArithmetics.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AnzanMegaArithmetics.Controllers
{
    [Authorize]
    public class SumaRestaController : Controller
    {
        [HttpGet]
        public IActionResult FormularioSR()
        {
            return View(new ConfSumaRestaModel());
        }

        [HttpPost]
        public IActionResult FormularioSR(ConfSumaRestaModel config)
        {
            return View(config);
        }

        [HttpPost]
        public IActionResult ConcentracionSR(ConfSumaRestaModel config)
        {
            config.CantidadEjercicios = Math.Max(1, config.CantidadEjercicios);
            config.TiempoMeditacion = Math.Max(0, config.TiempoMeditacion);

            TempData["CantidadEjercicios"] = config.CantidadEjercicios;
            TempData["NumeroOperaciones"] = config.NumeroOperaciones;
            TempData["MinDigitos"] = config.MinDigitos;
            TempData["MaxDigitos"] = config.MaxDigitos;
            TempData["VelocidadPreguntas"] = config.VelocidadPreguntas;
            TempData["ValorMaximo"] = config.ValorMaximo;
            TempData["UsarMaximoComoBase"] = config.UsarMaximoComoBase;
            TempData["TipoOperacion"] = config.TipoOperacion ?? "suma";
            TempData["TiempoMeditacion"] = config.TiempoMeditacion;

            TempData["DigitosSuma"] = config.DigitosSuma ?? "";
            TempData["DigitosResta"] = config.DigitosResta ?? "";
            TempData["DirectaSuma"] = config.DirectaSuma;
            TempData["DirectaResta"] = config.DirectaResta;

            TempData["EjerciciosRealizados"] = 0;
            TempData["Resultados"] = JsonSerializer.Serialize(new List<RSumaRestaModel>());

            ViewBag.TiempoMeditacion = config.TiempoMeditacion;
            ViewBag.VelocidadPreguntas = config.VelocidadPreguntas;

            return View("ConcentracionSR");
        }

        [HttpGet]
        public IActionResult EjercicioSR()
        {
            int cantidad = Convert.ToInt32(TempData["CantidadEjercicios"]);
            int realizados = Convert.ToInt32(TempData["EjerciciosRealizados"]);
            int numOperaciones = Convert.ToInt32(TempData["NumeroOperaciones"]);
            int minDig = Convert.ToInt32(TempData["MinDigitos"]);
            int maxDig = Convert.ToInt32(TempData["MaxDigitos"]);
            int valorMaximo = Convert.ToInt32(TempData["ValorMaximo"]);
            bool usarMax = Convert.ToBoolean(TempData["UsarMaximoComoBase"]);
            string tipoOperacion = TempData["TipoOperacion"].ToString();
            string velocidad = TempData["VelocidadPreguntas"].ToString();

            string[] sumaPermitidos = TempData["DigitosSuma"].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);
            string[] restaPermitidos = TempData["DigitosResta"].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);
            bool directaSuma = Convert.ToBoolean(TempData["DirectaSuma"]);
            bool directaResta = Convert.ToBoolean(TempData["DirectaResta"]);

            var resultadosJson = TempData["Resultados"] as string;
            var resultados = string.IsNullOrEmpty(resultadosJson)
                ? new List<RSumaRestaModel>()
                : JsonSerializer.Deserialize<List<RSumaRestaModel>>(resultadosJson);

            if (realizados >= cantidad)
            {
                TempData["Resultados"] = JsonSerializer.Serialize(resultados);
                return RedirectToAction("ResultadoSR");
            }

            var random = new Random();
            var numeros = new List<int>();
            var operaciones = new List<string>();

            for (int i = 0; i < numOperaciones; i++)
            {
                int valor;

                if (valorMaximo > 0 && usarMax && i == 0)
                {
                    // Primer número aleatorio entre 0 y valorMaximo
                    valor = random.Next(0, valorMaximo + 1);
                }
                else
                {
                    int digitos = random.Next(minDig, maxDig + 1);
                    int min = (int)Math.Pow(10, digitos - 1);
                    int max = (int)Math.Pow(10, digitos) - 1;
                    valor = random.Next(min, max + 1);
                }

                string op = tipoOperacion switch
                {
                    "suma" => "+",
                    "resta" => "-",
                    "ambos" => random.Next(0, 2) == 0 ? "+" : "-",
                    _ => "+"
                };

                numeros.Add(valor);
                operaciones.Add(op);
            }


            ViewBag.Numeros = numeros;
            ViewBag.Operaciones = operaciones;
            ViewBag.VelocidadPreguntas = velocidad;
            ViewBag.EjercicioActual = realizados + 1;
            ViewBag.TotalEjercicios = cantidad;

            TempData["Resultados"] = JsonSerializer.Serialize(resultados);
            TempData["EjerciciosRealizados"] = realizados;
            TempData["UltimosNumeros"] = JsonSerializer.Serialize(numeros);
            TempData["UltimasOperaciones"] = JsonSerializer.Serialize(operaciones);

            TempData.Keep();

            return View();
        }


        [HttpPost]
        public IActionResult ResultadoSR(int respuesta, string respondido)
        {
            bool respondio = respondido == "true";
            int ejerciciosRealizados = Convert.ToInt32(TempData["EjerciciosRealizados"]);

            var numeros = JsonSerializer.Deserialize<List<int>>(TempData["UltimosNumeros"].ToString());
            var operaciones = JsonSerializer.Deserialize<List<string>>(TempData["UltimasOperaciones"].ToString());

            int resultadoCorrecto = 0;
            for (int i = 0; i < numeros.Count; i++)
            {
                if (operaciones[i] == "-")
                    resultadoCorrecto -= numeros[i];
                else
                    resultadoCorrecto += numeros[i];
            }

            var resultadosJson = TempData["Resultados"] as string;
            var resultados = string.IsNullOrEmpty(resultadosJson)
                ? new List<RSumaRestaModel>()
                : JsonSerializer.Deserialize<List<RSumaRestaModel>>(resultadosJson);

            string operacionTexto = "";
            for (int i = 0; i < numeros.Count; i++)
            {
                string signo = operaciones[i];
                string num = numeros[i].ToString();
                operacionTexto += (i == 0 ? "" : signo) + num;
            }

            resultados.Add(new RSumaRestaModel
            {
                RespuestaUsuario = respondio ? respuesta : -1,
                RespuestaCorrecta = resultadoCorrecto,
                OperacionTexto = operacionTexto,
                Respondido = respondio
            });


            ejerciciosRealizados++;
            TempData["Resultados"] = JsonSerializer.Serialize(resultados);
            TempData["EjerciciosRealizados"] = ejerciciosRealizados;

            TempData.Keep("CantidadEjercicios");
            TempData.Keep("NumeroOperaciones");
            TempData.Keep("MinDigitos");
            TempData.Keep("MaxDigitos");
            TempData.Keep("VelocidadPreguntas");
            TempData.Keep("ValorMaximo");
            TempData.Keep("UsarMaximoComoBase");
            TempData.Keep("TipoOperacion");
            TempData.Keep("DigitosSuma");
            TempData.Keep("DigitosResta");
            TempData.Keep("DirectaSuma");
            TempData.Keep("DirectaResta");

            int cantidadTotal = Convert.ToInt32(TempData.Peek("CantidadEjercicios"));
            if (ejerciciosRealizados >= cantidadTotal)
            {
                return RedirectToAction("ResultadoSR");
            }

            return RedirectToAction("EjercicioSR");
        }

        [HttpGet]
        public IActionResult ResultadoSR()
        {
            TempData.Keep("Resultados");
            return View();
        }
    }
}
