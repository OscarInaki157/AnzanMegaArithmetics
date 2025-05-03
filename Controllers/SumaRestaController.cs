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

            TempData["ValorMaximo"] = config.ValorMaximo.ToString();

            TempData["UsarMaximoComoBase"] = config.UsarMaximoComoBase;
            TempData["TipoOperacion"] = config.TipoOperacion ?? "suma";
            TempData["TiempoMeditacion"] = config.TiempoMeditacion;

            TempData["DigitosSuma"] = (config.DigitosSuma ?? "").Replace("\r", "").Replace("\n", "").Trim();
            TempData["DigitosResta"] = (config.DigitosResta ?? "").Replace("\r", "").Replace("\n", "").Trim();

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

            long valorMaximo = Convert.ToInt64(TempData["ValorMaximo"]);

            bool usarMax = Convert.ToBoolean(TempData["UsarMaximoComoBase"]);
            string tipoOperacion = TempData["TipoOperacion"].ToString();
            string velocidad = TempData["VelocidadPreguntas"].ToString();

            string[] sumaPermitidos = TempData["DigitosSuma"].ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToArray();

            string[] restaPermitidos = TempData["DigitosResta"].ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToArray();

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
            var numeros = new List<long>();
            var operaciones = new List<string>();

            var listaSuma = GenerarNumerosValidos(sumaPermitidos, minDig, maxDig, valorMaximo);
            var listaResta = GenerarNumerosValidos(restaPermitidos, minDig, maxDig, valorMaximo);

            if (listaSuma.Count == 0 && tipoOperacion != "resta")
                listaSuma = sumaPermitidos.Select(d => int.Parse(d)).ToList();

            if (listaResta.Count == 0 && tipoOperacion != "suma")
                listaResta = restaPermitidos.Select(d => int.Parse(d)).ToList();

            for (int intento = 0; intento < 100; intento++)
            {
                numeros.Clear();
                operaciones.Clear();

                long primerValor, minValor, maxValor, maxRand, minRand;
                if (usarMax && valorMaximo > 0)
                {
                    maxRand = Math.Min(valorMaximo, long.MaxValue);
                    minRand = Math.Max(0, maxRand * (long)0.8);
                    primerValor = RandomLong(random, minRand, maxRand + 1);
                }
                else
                {
                    int digitos = random.Next(minDig, maxDig + 1);
                    minValor = (long)Math.Pow(10, digitos - 1);
                    maxValor = (long)Math.Pow(10, digitos) - 1;

                    if (valorMaximo > 0)
                        maxValor = Math.Min(maxValor, valorMaximo);

                    primerValor = RandomLong(random, minValor, maxValor + 1);
                }

                numeros.Add(primerValor);
                operaciones.Add("+"); 

                for (int i = 1; i < numOperaciones; i++)
                {
                    string op = tipoOperacion switch
                    {
                        "suma" => "+",
                        "resta" => "-",
                        "ambos" => random.Next(0, 2) == 0 ? "+" : "-",
                        _ => "+"
                    };

                    var listaActual = op == "+" ? listaSuma : listaResta;
                    int valor = listaActual[random.Next(listaActual.Count)];

                    numeros.Add(valor);
                    operaciones.Add(op);
                }

                long resultado = 0;
                for (int i = 0; i < numeros.Count; i++)
                {
                    resultado += operaciones[i] == "-" ? -numeros[i] : numeros[i];
                }

                if (resultado >= 0)
                    break; 
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

        private long RandomLong(Random rng, long min, long max)
        {
            if (min >= max) return min;
            byte[] buf = new byte[8];
            rng.NextBytes(buf);
            long longRand = Math.Abs(BitConverter.ToInt64(buf, 0));
            return min + (longRand % (max - min + 1));
        }

        private List<int> GenerarNumerosValidos(string[] digitosPermitidos, int minDig, int maxDig, long valorMax)
        {
            var resultados = new HashSet<int>();
            var digitos = digitosPermitidos.Distinct().ToArray();

            for (int longitud = minDig; longitud <= maxDig; longitud++)
            {
                foreach (var combinacion in ProductoCartesiano(digitos, longitud))
                {
                    var numStr = string.Concat(combinacion);
                    if (numStr.StartsWith("0")) continue;

                    int num = int.Parse(numStr);
                    if (valorMax > 0 && (long)num > valorMax) continue;

                    resultados.Add(num);
                }
            }

            return resultados.OrderBy(n => n).ToList();
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
        public IActionResult ResultadoSR(int respuesta, string respondido)
        {
            bool respondio = respondido == "true";
            int ejerciciosRealizados = Convert.ToInt32(TempData["EjerciciosRealizados"]);

            var numeros = JsonSerializer.Deserialize<List<long>>(TempData["UltimosNumeros"].ToString());
            var operaciones = JsonSerializer.Deserialize<List<string>>(TempData["UltimasOperaciones"].ToString());

            long resultadoCorrecto = 0;
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

        [HttpPost]
        public IActionResult FinalizarSR()
        {
            TempData.Keep("Resultados");
            return RedirectToAction("ResultadoSR");
        }

    }
}