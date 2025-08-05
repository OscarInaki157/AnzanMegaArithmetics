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
            TempData.Clear();
            var configJson = HttpContext.Session.GetString("UltimaConfigSR");
            ConfSumaRestaModel config;
            if (!string.IsNullOrEmpty(configJson))
            {
                config = JsonSerializer.Deserialize<ConfSumaRestaModel>(configJson);
            }
            else
            {
                config = new ConfSumaRestaModel
                {
                    DigitosSuma = "1,2,3,4,5,6,7,8,9",
                    DigitosResta = "1,2,3,4,5,6,7,8,9",
                    NumeroOperaciones = 4,
                    TiempoMeditacion = 3 
                };
            }

            ModelState.Clear();
            return View(config);
        }


        [HttpGet]
        public IActionResult LimpiarSumaRestaYDashboard()
        {
            TempData.Clear();
            HttpContext.Session.Remove("UltimaConfigSR");
            return RedirectToAction("Dashboard", "Dashboard");
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

            HttpContext.Session.SetString("UltimaConfigSR", JsonSerializer.Serialize(config));


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

            bool operacionesDirectas = directaSuma || directaResta || (tipoOperacion == "ambos" && directaSuma && directaResta);

            for (int intento = 0; intento < 100; intento++)
            {
                numeros.Clear();
                operaciones.Clear();

                // 1. Generación del primer número
                long primerValor;
                bool esSoloResta = tipoOperacion == "resta";

                if (esSoloResta)
                {
                    // Reemplazar RandomLong con GenerarNumeroNormal para restas
                    primerValor = GenerarNumeroNormal(
                        Math.Min(minDig + 1, maxDig),
                        maxDig,
                        valorMaximo,
                        restaPermitidos,
                        random);
                }
                else if (usarMax && valorMaximo > 0)
                {
                    // Usar valor máximo como base
                    primerValor = GenerarPrimerNumeroCercanoAlMaximo(valorMaximo, minDig, maxDig, tipoOperacion == "resta" ? restaPermitidos : sumaPermitidos, random);
                }
                else
                {
                    // Generación normal
                    primerValor = GenerarNumeroNormal(minDig, maxDig, valorMaximo,
                                                   tipoOperacion == "resta" ? restaPermitidos : sumaPermitidos,
                                                   random);
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

                    long nuevoNumero;
                    if (operacionesDirectas)
                    {
                        nuevoNumero = GenerarNumeroParaOperacionDirecta(
                            op == "+" ? sumaPermitidos : restaPermitidos,
                            minDig, maxDig, valorMaximo,
                            numeros, operaciones, op, random);
                    }
                    else
                    {
                        nuevoNumero = GenerarNumeroNormal(minDig, maxDig, valorMaximo,
                                                          op == "+" ? sumaPermitidos : restaPermitidos,
                                                          random);
                    }

                    numeros.Add(nuevoNumero);
                    operaciones.Add(op);
                }


                long resultado = CalcularResultado(numeros, operaciones);
                if (resultado >= 0 && (!esSoloResta || resultado > 0))
                {
                    if (!operacionesDirectas || OperacionDirectaEsValida(numeros, operaciones))
                    {
                        break;
                    }
                }
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
            TempData["StartTime"] = DateTime.UtcNow.ToString("O");

            TempData.Keep();

            return View();
        }


        private long GenerarPrimerNumeroCercanoAlMaximo(long valorMaximo, int minDig, int maxDig,
                                               string[] digitosPermitidos, Random random)
        {
            if (minDig == 1 && maxDig == 1)
            {
                var digitosValidos = digitosPermitidos
                    .Where(d => d.Length == 1 && d != "0")
                    .Select(int.Parse)
                    .Where(n => n <= valorMaximo)
                    .ToList();

                return digitosValidos.Any() ? digitosValidos[random.Next(digitosValidos.Count)] : 1;
            }

            // Generar número con dígitos permitidos cercano al máximo
            string maxStr = valorMaximo.ToString();
            string numeroStr = "";

            for (int i = 0; i < maxStr.Length; i++)
            {
                char maxDigito = maxStr[i];
                var permitidos = digitosPermitidos.Select(d => d[0])
                                       .Where(d => d <= maxDigito)
                                       .ToArray();

                if (permitidos.Any())
                {
                    char digito = permitidos[random.Next(permitidos.Length)];
                    numeroStr += digito;
                }
                else
                {
                    // Si no hay dígitos permitidos menores, usar el máximo permitido menor que el actual
                    var maxPermitido = digitosPermitidos.Select(d => d[0])
                                            .Where(d => d < maxDigito)
                                            .DefaultIfEmpty('0')
                                            .Max();
                    numeroStr += maxPermitido;
                    // Completar con los mayores dígitos permitidos
                    for (int j = i + 1; j < maxStr.Length; j++)
                    {
                        numeroStr += digitosPermitidos.Select(d => d[0]).Max();
                    }
                    break;
                }
            }

            long numero = long.Parse(numeroStr);

            // Asegurar que tenga al menos minDig dígitos
            if (numeroStr.Length < minDig)
            {
                string extra = "";
                for (int i = 0; i < minDig - numeroStr.Length; i++)
                {
                    extra += digitosPermitidos[random.Next(digitosPermitidos.Length)];
                }
                numero = long.Parse(numeroStr + extra);
            }

            return numero;
        }

        private long GenerarNumeroNormal(int minDig, int maxDig, long valorMaximo, string[] digitosPermitidos, Random random)
        {
            if (minDig == 1 && maxDig == 1)
            {
                var digitosValidos = digitosPermitidos
                    .Where(d => d.Length == 1 && d != "0")
                    .Select(int.Parse)
                    .Where(n => valorMaximo <= 0 || n <= valorMaximo)
                    .ToList();

                return digitosValidos.Any() ? digitosValidos[random.Next(digitosValidos.Count)] : 1;
            }

            // Generar número asegurando que todos sus dígitos estén permitidos
            string numeroStr = "";
            int digitos = random.Next(minDig, maxDig + 1);

            // Primer dígito no puede ser cero
            var primerosDigitos = digitosPermitidos.Where(d => d != "0").ToArray();
            if (!primerosDigitos.Any()) primerosDigitos = new[] { "1" };
            numeroStr += primerosDigitos[random.Next(primerosDigitos.Length)];

            // Resto de dígitos
            for (int i = 1; i < digitos; i++)
            {
                numeroStr += digitosPermitidos[random.Next(digitosPermitidos.Length)];
            }

            long numero = long.Parse(numeroStr);

            // Ajustar según valor máximo si está configurado
            if (valorMaximo > 0 && numero > valorMaximo)
            {
                // Si excede, generar uno más pequeño
                return GenerarNumeroNormal(minDig, Math.Min(maxDig, valorMaximo.ToString().Length),
                       valorMaximo, digitosPermitidos, random);
            }

            return numero;
        }

        private long GenerarNumeroParaOperacionDirecta(string[] digitosPermitidos, int minDig, int maxDig, long valorMaximo, List<long> numerosExist, List<string> operacionesExist, string nuevaOperacion, Random random)
        {
            // Primero generamos un número normal
            long numero = GenerarNumeroNormal(minDig, maxDig, valorMaximo, digitosPermitidos, random);

            // Luego verificamos si cumple con las reglas de operación directa
            if (OperacionDirectaEsValida(numerosExist.Concat(new[] { numero }).ToList(),
                                        operacionesExist.Concat(new[] { nuevaOperacion }).ToList()))
            {
                return numero;
            }

            // Si no cumple, generamos uno más pequeño
            return GenerarNumeroNormal(minDig, Math.Min(maxDig, numerosExist.Last().ToString().Length),
                                      valorMaximo, digitosPermitidos, random);
        }

        private bool OperacionDirectaEsValida(List<long> numeros, List<string> operaciones)
        {
            // Convertimos todos los números a strings de igual longitud
            int maxLength = numeros.Max(n => n.ToString().Length);
            var numerosStr = numeros.Select(n => n.ToString().PadLeft(maxLength, '0')).ToList();

            // Verificamos cada columna
            for (int i = 0; i < maxLength; i++)
            {
                int sumaColumna = 0;
                for (int j = 0; j < numerosStr.Count; j++)
                {
                    int digito = int.Parse(numerosStr[j][i].ToString());
                    sumaColumna += operaciones[j] == "+" ? digito : -digito;
                }

                if (sumaColumna < 0 || sumaColumna > 9)
                {
                    return false;
                }
            }

            return true;
        }

        private long CalcularResultado(List<long> numeros, List<string> operaciones)
        {
            long resultado = 0;
            for (int i = 0; i < numeros.Count; i++)
            {
                resultado += operaciones[i] == "-" ? -numeros[i] : numeros[i];
            }
            return resultado;
        }


        private long RandomLong(Random rng, long min, long max)
        {
            if (min >= max) return min;

            long range = max - min;
            if (range == 0) return min;

            byte[] buf = new byte[8];
            rng.NextBytes(buf);
            long longRand = Math.Abs(BitConverter.ToInt64(buf, 0));

            return min + (longRand % range);
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

            var startIso = TempData["StartTime"]?.ToString();
            DateTime startTime = DateTime.Parse(startIso, null, System.Globalization.DateTimeStyles.RoundtripKind);
            double tiempoSegundos = (DateTime.UtcNow - startTime).TotalSeconds;

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
                Respondido = respondio,
                TiempoRespuesta = respondio ? tiempoSegundos : 0
            });


            ejerciciosRealizados++;
            TempData["Resultados"] = JsonSerializer.Serialize(resultados);
            TempData["EjerciciosRealizados"] = ejerciciosRealizados;

            TempData["StartTime"] = DateTime.UtcNow.ToString("O");
            TempData.Keep("StartTime");

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

            var resultadosJson = TempData["Resultados"] as string;
            var resultados = string.IsNullOrEmpty(resultadosJson)
                ? new List<RSumaRestaModel>()
                : JsonSerializer.Deserialize<List<RSumaRestaModel>>(resultadosJson);

            TempData.Keep("Resultados");
            return View(resultados);
        }

        [HttpPost]
        public IActionResult FinalizarSR(int respuesta, string respondido)
        {
            var startIso = TempData["StartTime"]?.ToString();
            DateTime startTime = DateTime.Parse(startIso, null, System.Globalization.DateTimeStyles.RoundtripKind);
            double tiempoSegundos = (DateTime.UtcNow - startTime).TotalSeconds;

            bool respondio = respondido == "true";
            var resultadosJson = TempData["Resultados"] as string;
            var resultados = string.IsNullOrEmpty(resultadosJson)
                ? new List<RSumaRestaModel>()
                : JsonSerializer.Deserialize<List<RSumaRestaModel>>(resultadosJson);

            var numeros = JsonSerializer.Deserialize<List<long>>(TempData["UltimosNumeros"].ToString());
            var operaciones = JsonSerializer.Deserialize<List<string>>(TempData["UltimasOperaciones"].ToString());

            long resultadoCorrecto = 0;
            for (int i = 0; i < numeros.Count; i++)
            {
                resultadoCorrecto += operaciones[i] == "-" ? -numeros[i] : numeros[i];
            }

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
                Respondido = respondio,
                TiempoRespuesta = respondio ? tiempoSegundos : 0
            });

            int ejerciciosRealizados = resultados.Count;
            int cantidadEjercicios = Convert.ToInt32(TempData.Peek("CantidadEjercicios"));

            for (int i = ejerciciosRealizados; i < cantidadEjercicios; i++)
            {
                resultados.Add(new RSumaRestaModel
                {
                    RespuestaUsuario = -1,
                    RespuestaCorrecta = 0,
                    OperacionTexto = "No realizado",
                    Respondido = false
                });
            }

            TempData["Resultados"] = JsonSerializer.Serialize(resultados);
            return RedirectToAction("ResultadoSR");
        }

        [HttpPost]
        public IActionResult RepetirEjercicioSR()
        {
            var configJson = HttpContext.Session.GetString("UltimaConfigSR");
            if (string.IsNullOrEmpty(configJson))
            {
                return RedirectToAction("FormularioSR");
            }

            var config = JsonSerializer.Deserialize<ConfSumaRestaModel>(configJson);

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

            return RedirectToAction("EjercicioSR");
        }


    }
}