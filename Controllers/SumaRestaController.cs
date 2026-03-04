using AnzanMegaArithmetics.Models;
using AnzanMegaArithmetics.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AnzanMegaArithmetics.Controllers
{
    [Authorize]
    public class SumaRestaController : Controller
    {
        private readonly IPruebasDBService _pruebasDBService;
        public SumaRestaController(IPruebasDBService pruebasDBService) 
        {
            this._pruebasDBService = pruebasDBService;
        }

        [HttpGet]
        public IActionResult FormularioSR()
        {
            TempData.Clear();

            var userId = HttpContext.Session.GetInt32("Id_Usuario");

            if (userId == 0 || userId == null) 
            {
                return RedirectToAction("Inicio", "Inicio");
            }

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

            if (config.ValorMaximo > 0)
            {
                // Calculamos el valor más bajo posible que se puede generar con los 'MinDigitos' pedidos
                // Ejemplo: Si pide mínimo 2 dígitos, el valor más bajo es 10. Si pide 3, es 100.
                long valorMinimoPosible = config.MinDigitos > 1 ? (long)Math.Pow(10, config.MinDigitos - 1) : 1;

                // 1. Regla Básica: ¿El mínimo de dígitos exige un número mayor al Valor Máximo?
                if (valorMinimoPosible > config.ValorMaximo)
                {
                    TempData["ErrorMessage"] = $"Configuración imposible: Pides números de mínimo {config.MinDigitos} dígitos (el menor es {valorMinimoPosible}), pero tu Valor Máximo es apenas {config.ValorMaximo}.";
                    return View("FormularioSR", config);
                }

                // 2. Regla de Suma: ¿El total de operaciones excede el máximo incluso usando los números más pequeños?
                if (config.TipoOperacion == "suma" || config.TipoOperacion == "ambos")
                {
                    long sumaMinimaAbsoluta = config.NumeroOperaciones * valorMinimoPosible;

                    if (config.TipoOperacion == "suma" && sumaMinimaAbsoluta > config.ValorMaximo)
                    {
                        TempData["ErrorMessage"] = $"Matemáticamente imposible: {config.NumeroOperaciones} números de al menos {config.MinDigitos} dígitos sumarán como mínimo {sumaMinimaAbsoluta}, superando tu límite de {config.ValorMaximo}.";
                        return View("FormularioSR", config);
                    }
                }

                // 3. Regla de Resta: Para no bajar de 0, el primer número debe poder soportar todas las restas siguientes
                if (config.TipoOperacion == "resta")
                {
                    // El total mínimo que le vamos a restar al primer número
                    long restasMinimasAbsolutas = (config.NumeroOperaciones - 1) * valorMinimoPosible;

                    if (restasMinimasAbsolutas > config.ValorMaximo)
                    {
                        TempData["ErrorMessage"] = $"Matemáticamente imposible: Para hacer {config.NumeroOperaciones - 1} restas de {config.MinDigitos} dígitos sin bajar de cero, el primer número debe ser mayor a {restasMinimasAbsolutas}, superando tu límite de {config.ValorMaximo}.";
                        return View("FormularioSR", config);
                    }
                }
            }

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

            bool operacionesDirectas = directaSuma || directaResta || (tipoOperacion == "ambos" && directaSuma && directaResta);
            bool esSoloResta = tipoOperacion == "resta";



            // 1. Calcular el Ancla (aprox. 60% del máximo)
            long anchorValue = 0;
            int posAncla = -1;

            if (valorMaximo > 0)
            {
                double varianza = 0.55 + (random.NextDouble() * 0.10);
                anchorValue = (long)(valorMaximo * varianza);
                if (anchorValue < 1) anchorValue = 1;

                posAncla = usarMax ? 0 : random.Next(0, numOperaciones);
            }

            for (int intento = 0; intento < 300; intento++)
            {
                numeros.Clear();
                operaciones.Clear();
                long acumulado = 0;
                bool intentoValido = true;

                // Respetamos estrictamente el loop por el número de operaciones solicitadas
                for (int i = 0; i < numOperaciones; i++)
                {
                    // A. DETERMINAR OPERADOR ESTRICTO
                    string op = "+";
                    if (i > 0)
                    {
                        if (tipoOperacion == "suma") op = "+";
                        else if (tipoOperacion == "resta") op = "-";
                        else op = random.Next(0, 2) == 0 ? "+" : "-"; // "ambos"
                    }

                    // B. DETERMINAR DÍGITOS PERMITIDOS SEGÚN POSICIÓN
                    string[] digitosParaGenerar;
                    if (i == 0)
                    {
                        digitosParaGenerar = new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9" };
                    }
                    else
                    {

                        digitosParaGenerar = op == "+" ? sumaPermitidos : restaPermitidos;

                        if (digitosParaGenerar.Length == 1 && digitosParaGenerar[0] != "0")
                        {
                            digitosParaGenerar = new[] { digitosParaGenerar[0], "0" };
                        }
                    }

                    long nuevoNumero = 0;

                    // CASO 1: ES LA POSICIÓN DEL ANCLA Y HAY VALOR MÁXIMO
                    if (valorMaximo > 0 && i == posAncla)
                    {
                        nuevoNumero = anchorValue;

                        // Intentar salvar el signo si es "ambos", pero si es estrictamente "suma" o "resta", no se cambia
                        if (i > 0 && tipoOperacion == "ambos")
                        {
                            if (op == "+" && acumulado + nuevoNumero > valorMaximo) op = "-";
                            if (op == "-" && acumulado - nuevoNumero < 0) op = "+";
                        }

                        if ((op == "+" && acumulado + nuevoNumero > valorMaximo) ||
                            (op == "-" && acumulado - nuevoNumero < 0))
                        {
                            intentoValido = false; break;
                        }
                    }
                    // CASO 2: NÚMERO NORMAL (Ajustado al acumulador)
                    else
                    {
                        long topeMaximo = 0;
                        if (valorMaximo > 0)
                        {
                            topeMaximo = op == "+" ? valorMaximo - acumulado : acumulado;
                        }

                        if (valorMaximo > 0)
                        {
                            long valorMinimoPosible = minDig > 1 ? (long)Math.Pow(10, minDig - 1) : 1;
                            if (topeMaximo < valorMinimoPosible) { intentoValido = false; break; }
                        }

                        long maxParaGenerar = valorMaximo > 0 ? topeMaximo : 0;

                        // Condición especial de protección: Si es el primer número, no hay máximo global, y hay restas
                        if (i == 0 && valorMaximo == 0 && (tipoOperacion == "resta" || tipoOperacion == "ambos"))
                        {
                            long maxPosibleResta = (long)Math.Pow(10, maxDig) - 1;
                            long restaEstimada = maxPosibleResta * numOperaciones;

                            nuevoNumero = GenerarNumeroNormal(minDig, Math.Min(maxDig + 2, 10), 0, digitosParaGenerar, random);

                            // Si el número generado es muy pequeño para soportar futuras restas, lo inflamos
                            if (nuevoNumero <= restaEstimada)
                            {
                                nuevoNumero += restaEstimada + random.Next(1, 1000);
                            }
                        }
                        else
                        {
                            if (operacionesDirectas && i > 0) // El primero rara vez necesita FingerMath porque es base
                            {
                                nuevoNumero = GenerarNumeroParaOperacionDirecta(digitosParaGenerar, minDig, maxDig, maxParaGenerar, numeros, operaciones, op, random);
                            }
                            else
                            {
                                nuevoNumero = GenerarNumeroNormal(minDig, maxDig, maxParaGenerar, digitosParaGenerar, random);
                            }
                        }

                        if (valorMaximo > 0 && nuevoNumero > topeMaximo) { intentoValido = false; break; }
                    }

                    numeros.Add(nuevoNumero);
                    operaciones.Add(op);
                    acumulado += (op == "+") ? nuevoNumero : -nuevoNumero;

                    if (acumulado < 0) { intentoValido = false; break; }
                }

                if (intentoValido && acumulado >= 0 && (!esSoloResta || acumulado > 0))
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

        private long GenerarNumeroNormal(int minDig, int maxDig, long valorMaximo, string[] digitosPermitidos, Random random)
        {
            if (valorMaximo > 0)
            {
                int maxDigitosPosibles = valorMaximo.ToString().Length;
                maxDig = Math.Min(maxDig, maxDigitosPosibles);
                minDig = Math.Min(minDig, maxDig);
            }

            if (minDig == 1 && maxDig == 1)
            {
                var digitosValidos = digitosPermitidos
                    .Where(d => d.Length == 1 && d != "0")
                    .Select(int.Parse)
                    .Where(n => valorMaximo <= 0 || n <= valorMaximo)
                    .ToList();

                return digitosValidos.Any() ? digitosValidos[random.Next(digitosValidos.Count)] : 1;
            }

            long numero = 0;
            int intentos = 0;

            do
            {
                string numeroStr = "";
                var primerosDigitos = digitosPermitidos.Where(d => d != "0").ToArray();
                if (!primerosDigitos.Any()) primerosDigitos = new[] { "1" };

                numeroStr += primerosDigitos[random.Next(primerosDigitos.Length)];

                for (int i = 1; i < minDig; i++)
                {
                    numeroStr += digitosPermitidos[random.Next(digitosPermitidos.Length)];
                }

                int digitosAdicionales = random.Next(0, maxDig - minDig + 1);
                for (int i = 0; i < digitosAdicionales; i++)
                {
                    numeroStr += digitosPermitidos[random.Next(digitosPermitidos.Length)];
                }

                numero = long.Parse(numeroStr);
                intentos++;

            } while (valorMaximo > 0 && numero > valorMaximo && intentos < 20);

            if (valorMaximo > 0 && numero > valorMaximo)
            {
                long baseMin = (long)Math.Pow(10, minDig - 1);
                long techo = Math.Max(baseMin, valorMaximo);
                return (long)(random.NextDouble() * (techo - baseMin) + baseMin);
            }

            return numero;
        }

        private long GenerarNumeroParaOperacionDirecta(string[] digitosPermitidos, int minDig, int maxDig, long valorMaximo,
                                             List<long> numerosExist, List<string> operacionesExist,
                                             string nuevaOperacion, Random random)
        {
            for (int intento = 0; intento < 50; intento++)
            {
                long numero = GenerarNumeroFingerMath(minDig, maxDig, valorMaximo, digitosPermitidos,
                                                    numerosExist, operacionesExist, nuevaOperacion, random);

                if (numero > 0)
                {
                    var nuevosNumeros = new List<long>(numerosExist) { numero };
                    var nuevasOperaciones = new List<string>(operacionesExist) { nuevaOperacion };

                    if (OperacionDirectaEsValida(nuevosNumeros, nuevasOperaciones)) return numero;
                }
            }

            for (int intento = 0; intento < 50; intento++)
            {
                long numero = GenerarNumeroNormal(minDig, maxDig, valorMaximo, digitosPermitidos, random);

                var nuevosNumeros = new List<long>(numerosExist) { numero };
                var nuevasOperaciones = new List<string>(operacionesExist) { nuevaOperacion };

                if (OperacionDirectaEsValida(nuevosNumeros, nuevasOperaciones)) return numero;
            }

            if (nuevaOperacion == "+") return GenerarNumeroSeguroSuma(digitosPermitidos, minDig, valorMaximo, random);
            else return GenerarNumeroSeguroResta(valorMaximo, minDig, random);
        }

        private long GenerarNumeroSeguroSuma(string[] digitosPermitidos, int minDig, long valorMaximo, Random random)
        {
            if (valorMaximo > 0)
            {
                int maxDigitosPosibles = valorMaximo.ToString().Length;
                minDig = Math.Min(minDig, maxDigitosPosibles);
            }

            string numeroStr = random.Next(1, 5).ToString();
            for (int i = 1; i < minDig; i++) numeroStr += random.Next(0, 5).ToString();

            long numero = long.Parse(numeroStr);
            if (valorMaximo > 0 && numero > valorMaximo) return Math.Max(1, valorMaximo);
            return numero;
        }

        private long GenerarNumeroSeguroResta(long acumuladoDisponible, int minDig, Random random)
        {
            long maxResta = Math.Min(acumuladoDisponible, (long)Math.Pow(10, minDig) - 1);
            if (maxResta <= 0) return 0;
            long numero = (long)Math.Pow(10, minDig - 1) + random.Next(0, (int)Math.Pow(10, minDig - 1));
            return Math.Min(numero, maxResta);
        }

        private long GenerarNumeroFingerMath(int minDig, int maxDig, long valorMaximo, string[] digitosPermitidos,
                           List<long> numerosExist, List<string> operacionesExist,
                           string nuevaOperacion, Random random)
        {
            if (valorMaximo > 0)
            {
                int maxDigitosPosibles = valorMaximo.ToString().Length;
                maxDig = Math.Min(maxDig, maxDigitosPosibles);
                minDig = Math.Min(minDig, maxDig);
            }

            int columnas = maxDig;
            string numeroStr = "";
            bool[] pulgarUsadoPorColumna = new bool[columnas];
            int[] acumuladoPorColumna = new int[columnas];

            for (int col = 0; col < columnas; col++)
            {
                foreach (var (num, op) in numerosExist.Zip(operacionesExist, (n, o) => (n, o)))
                {
                    string numStr = num.ToString().PadLeft(columnas, '0');
                    int digito = int.Parse(numStr[numStr.Length - 1 - col].ToString());

                    if (op == "+")
                    {
                        if (digito >= 5) { pulgarUsadoPorColumna[col] = true; digito -= 5; }
                        acumuladoPorColumna[col] += digito;
                    }
                    else
                    {
                        if (digito >= 5) { pulgarUsadoPorColumna[col] = false; digito -= 5; }
                        acumuladoPorColumna[col] -= digito;
                    }
                }
            }

            for (int col = 0; col < columnas; col++)
            {
                var digitosPosibles = digitosPermitidos.Select(d => int.Parse(d))
                    .Where(d => {
                        bool usarPulgar = d >= 5;
                        int valorDigito = usarPulgar ? d - 5 : d;

                        if (nuevaOperacion == "+")
                        {
                            if (usarPulgar && pulgarUsadoPorColumna[col]) return false;
                            int nuevoAcumulado = acumuladoPorColumna[col] + valorDigito;
                            bool nuevoPulgar = pulgarUsadoPorColumna[col] || usarPulgar;
                            return nuevoAcumulado <= 4;
                        }
                        else
                        {
                            if (usarPulgar && !pulgarUsadoPorColumna[col]) return false;
                            int nuevoAcumulado = acumuladoPorColumna[col] - valorDigito;
                            return nuevoAcumulado >= 0;
                        }
                    }).OrderBy(d => d).ToList();

                if (!digitosPosibles.Any()) digitosPosibles = new List<int> { 1, 2, 3, 4 };

                int index;
                if (digitosPosibles.Count > 3)
                {
                    double r = random.NextDouble();
                    index = (int)Math.Floor(digitosPosibles.Count * (1 - Math.Sqrt(1 - r)));
                }
                else index = random.Next(digitosPosibles.Count);

                numeroStr = digitosPosibles[index].ToString() + numeroStr;
            }

            while (numeroStr.Length < minDig) numeroStr = "1" + numeroStr;
            if (numeroStr.Length > maxDig) numeroStr = numeroStr.Substring(0, maxDig);

            long numero = long.Parse(numeroStr);

            if (valorMaximo > 0 && numero > valorMaximo) return -1;
            return numero;
        }

        private bool OperacionDirectaEsValida(List<long> numeros, List<string> operaciones)
        {
            int maxLength = numeros.Max(n => n.ToString().Length);
            var numerosStr = numeros.Select(n => n.ToString().PadLeft(maxLength, '0')).ToList();

            long resultadoTotal = CalcularResultado(numeros, operaciones);
            if (resultadoTotal < 0) return false;

            for (int columna = 0; columna < maxLength; columna++)
            {
                int acumulado = 0;
                bool pulgarAbajo = false;

                for (int i = 0; i < numerosStr.Count; i++)
                {
                    int digito = int.Parse(numerosStr[i][columna].ToString());
                    bool esSuma = operaciones[i] == "+";

                    if (esSuma)
                    {
                        if (digito >= 5)
                        {
                            if (pulgarAbajo) return false;
                            pulgarAbajo = true;
                            digito -= 5;
                        }
                        acumulado += digito;
                        if (acumulado > 4) return false;
                    }
                    else
                    {
                        if (digito >= 5)
                        {
                            if (!pulgarAbajo) return false;
                            pulgarAbajo = false;
                            digito -= 5;
                        }
                        acumulado -= digito;
                        if (acumulado < 0) return false;
                        if (acumulado > 4) return false;
                    }
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
                TiempoRespuesta = tiempoSegundos
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

            var userId = HttpContext.Session.GetInt32("Id_Usuario");
            if (userId == null || userId == 0) 
            {
                return RedirectToAction("Inicio", "Inicio");
            }

            var resultadosJson = TempData["Resultados"] as string;
            var resultados = string.IsNullOrEmpty(resultadosJson)
                ? new List<RSumaRestaModel>()
                : JsonSerializer.Deserialize<List<RSumaRestaModel>>(resultadosJson);

            int cantidadEjercicios = Convert.ToInt32(TempData.Peek("CantidadEjercicios"));

            int correctos = resultados.Count(r => r.RespuestaUsuario == r.RespuestaCorrecta);
            int incorrectos = resultados.Count(r => r.RespuestaUsuario != -1 && r.RespuestaUsuario != r.RespuestaCorrecta);

            int porcentaje = cantidadEjercicios > 0 ? (correctos * 100) / cantidadEjercicios : 0;
            int xp = porcentaje;

            PruebasDBModel results = new PruebasDBModel
            {
                Id_Usuario = userId.Value,
                Tiempo = TimeSpan.FromSeconds(resultados.Sum(r => r.TiempoRespuesta)),
                Total_Preguntas = cantidadEjercicios,
                Respuestas_Correctas = correctos,
                Fecha = DateTime.UtcNow,
                ExperienciaAdquirida = xp,
                Tipo_Prueba = "Suma Resta"
            };

            bool InsertarPrueba = _pruebasDBService.GuardarPrueba(results);

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
                TiempoRespuesta = tiempoSegundos
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