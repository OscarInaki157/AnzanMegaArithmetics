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

            // Verificar si solo hay un dígito permitido para el tipo de operación
            bool soloUnDigitoPermitido = false;
            string digitoPermitidoOriginal = "";
            string[] digitosRelevantes = tipoOperacion == "resta" ? restaPermitidos : sumaPermitidos;
            if (digitosRelevantes.Length == 1 && digitosRelevantes[0].Length == 1)
            {
                soloUnDigitoPermitido = true;
                digitoPermitidoOriginal = digitosRelevantes[0];
            }

            for (int intento = 0; intento < 100; intento++)
            {
                numeros.Clear();
                operaciones.Clear();

                // 1. Generación del primer número
                long primerValor;
                bool esSoloResta = tipoOperacion == "resta";

                // Caso especial: si solo hay un dígito permitido, generar primer número con dígitos aleatorios
                if (soloUnDigitoPermitido)
                {
                    // Para el primer número, usar TODOS los dígitos permitidos (1-9) para crear variedad
                    string[] todosDigitos = { "1", "2", "3", "4", "5", "6", "7", "8", "9" };

                    if (esSoloResta)
                    {
                        // Para restas, asegurar que el primer número sea suficientemente grande
                        int digitosPrimerNumero = Math.Min(maxDig + 2, 10); // Un poco más de dígitos
                        primerValor = GenerarNumeroNormal(
                            digitosPrimerNumero,
                            digitosPrimerNumero,
                            valorMaximo,
                            todosDigitos, // Usar todos los dígitos para variedad
                            random);

                        // Asegurar adicionalmente que sea mayor que cualquier posible resta posterior
                        long maxPosibleResta = (long)Math.Pow(10, maxDig) - 1;
                        long restaTotalEstimada = maxPosibleResta * (numOperaciones - 1);
                        if (primerValor <= restaTotalEstimada)
                        {
                            primerValor = restaTotalEstimada + random.Next(1, 10000);
                        }
                    }
                    else if (tipoOperacion == "ambos")
                    {
                        
                        int digitosExtra = Math.Min(maxDig + 2, 10); // Máximo 10 dígitos
                        primerValor = GenerarNumeroNormal(
                            digitosExtra,
                            digitosExtra,
                            valorMaximo,
                            todosDigitos, // Usar todos los dígitos
                            random);

                        long maxPosibleResta = (long)Math.Pow(10, maxDig) - 1;
                        long restaTotalEstimada = maxPosibleResta * (numOperaciones / 2); // Estimación conservadora
                        if (primerValor <= restaTotalEstimada)
                        {
                            primerValor = restaTotalEstimada + random.Next(1, 10000);
                        }
                    }

                    else if (usarMax && valorMaximo > 0)
                    {
                        // Usar valor máximo como base con todos los dígitos
                        primerValor = GenerarPrimerNumeroCercanoAlMaximo(valorMaximo,
                            minDig,
                            maxDig,
                            todosDigitos, random);
                    }
                    else
                    {
                        // Generación normal con todos los dígitos para variedad
                        primerValor = GenerarNumeroNormal(
                            minDig,
                            maxDig,
                            valorMaximo,
                            todosDigitos, // Usar todos los dígitos
                            random);
                    }
                }
                else if (esSoloResta)
                {
                    // Para restas, asegurar que el primer número sea suficientemente grande
                    int digitosPrimerNumero = Math.Min(maxDig + 2, 10); // No más de 10 dígitos
                    primerValor = GenerarNumeroNormal(
                        digitosPrimerNumero, // Usar más dígitos que el máximo permitido
                        digitosPrimerNumero,
                        valorMaximo,
                        restaPermitidos,
                        random);

                    // Asegurar adicionalmente que sea mayor que cualquier posible resta posterior
                    long maxPosibleResta = (long)Math.Pow(10, maxDig) - 1;
                    if (primerValor <= maxPosibleResta)
                    {
                        primerValor += maxPosibleResta;
                    }
                }
                else if (usarMax && valorMaximo > 0)
                {
                    // Usar valor máximo como base
                    primerValor = GenerarPrimerNumeroCercanoAlMaximo(valorMaximo, minDig, maxDig,
                                                   tipoOperacion == "resta" ? restaPermitidos : sumaPermitidos,
                                                   random);
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
                        // Para números siguientes, usar el dígito permitido original si aplica
                        string[] digitosParaOperacion = op == "+" ? sumaPermitidos : restaPermitidos;
                        if (soloUnDigitoPermitido)
                        {
                            digitosParaOperacion = new[] { digitoPermitidoOriginal };
                        }

                        nuevoNumero = GenerarNumeroParaOperacionDirecta(
                            digitosParaOperacion,
                            minDig, maxDig, valorMaximo,
                            numeros, operaciones, op, random);
                    }
                    else
                    {
                        // Para números siguientes, usar el dígito permitido original si aplica
                        string[] digitosParaOperacion = op == "+" ? sumaPermitidos : restaPermitidos;
                        if (soloUnDigitoPermitido)
                        {
                            digitosParaOperacion = new[] { digitoPermitidoOriginal };
                        }

                        nuevoNumero = GenerarNumeroNormal(minDig, maxDig, valorMaximo,
                                                          digitosParaOperacion,
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

            string maxStr = valorMaximo.ToString();
            string numeroStr = "";

            // Asegurar que tenga al menos minDig dígitos
            int digitosGenerados = 0;

            for (int i = 0; i < Math.Max(minDig, maxStr.Length); i++)
            {
                char maxDigito = i < maxStr.Length ? maxStr[i] : '9';
                var permitidos = digitosPermitidos.Select(d => d[0])
                                       .Where(d => d <= maxDigito)
                                       .ToArray();

                if (permitidos.Any())
                {
                    char digito = permitidos[random.Next(permitidos.Length)];
                    numeroStr += digito;
                    digitosGenerados++;
                }
                else
                {
                    var maxPermitido = digitosPermitidos.Select(d => d[0])
                                            .Where(d => d < maxDigito)
                                            .DefaultIfEmpty('0')
                                            .Max();
                    numeroStr += maxPermitido;
                    digitosGenerados++;
                }

                // Si ya tenemos minDig dígitos y hemos alcanzado la longitud máxima, salir
                if (digitosGenerados >= minDig && (i >= maxStr.Length - 1 || digitosGenerados >= maxDig))
                    break;
            }

            // Si aún no tenemos suficientes dígitos, completar
            while (digitosGenerados < minDig)
            {
                numeroStr += digitosPermitidos[random.Next(digitosPermitidos.Length)];
                digitosGenerados++;
            }

            // Si tenemos más dígitos que el máximo, truncar
            if (numeroStr.Length > maxDig)
            {
                numeroStr = numeroStr.Substring(0, maxDig);
            }

            return long.Parse(numeroStr);
        }

        private long GenerarNumeroNormal(int minDig, int maxDig, long valorMaximo, string[] digitosPermitidos, Random random)
        {
            // Si minDig y maxDig son 1, manejamos como caso especial
            if (minDig == 1 && maxDig == 1)
            {
                var digitosValidos = digitosPermitidos
                    .Where(d => d.Length == 1 && d != "0")
                    .Select(int.Parse)
                    .Where(n => valorMaximo <= 0 || n <= valorMaximo)
                    .ToList();

                return digitosValidos.Any() ? digitosValidos[random.Next(digitosValidos.Count)] : 1;
            }

            // Para otros casos, aseguramos que el número tenga exactamente minDig dígitos
            string numeroStr = "";

            // Primer dígito no puede ser cero
            var primerosDigitos = digitosPermitidos.Where(d => d != "0").ToArray();
            if (!primerosDigitos.Any()) primerosDigitos = new[] { "1" };
            numeroStr += primerosDigitos[random.Next(primerosDigitos.Length)];

            // Resto de dígitos (aseguramos que tenga exactamente minDig dígitos)
            for (int i = 1; i < minDig; i++)
            {
                numeroStr += digitosPermitidos[random.Next(digitosPermitidos.Length)];
            }

            // Si maxDig > minDig, podemos agregar dígitos adicionales al azar
            int digitosAdicionales = random.Next(0, maxDig - minDig + 1);
            for (int i = 0; i < digitosAdicionales; i++)
            {
                numeroStr += digitosPermitidos[random.Next(digitosPermitidos.Length)];
            }

            long numero = long.Parse(numeroStr);

            // Ajustar según valor máximo si está configurado
            if (valorMaximo > 0 && numero > valorMaximo)
            {
                return GenerarNumeroNormal(minDig, Math.Min(maxDig, valorMaximo.ToString().Length),
                       valorMaximo, digitosPermitidos, random);
            }

            return numero;
        }

        private long GenerarNumeroParaOperacionDirecta(string[] digitosPermitidos, int minDig, int maxDig, long valorMaximo,
                                     List<long> numerosExist, List<string> operacionesExist,
                                     string nuevaOperacion, Random random)
        {
            // Primero intentar con el nuevo método FingerMath
            for (int intento = 0; intento < 50; intento++)
            {
                long numero = GenerarNumeroFingerMath(minDig, maxDig, valorMaximo, digitosPermitidos,
                                                    numerosExist, operacionesExist, nuevaOperacion, random);

                var nuevosNumeros = new List<long>(numerosExist) { numero };
                var nuevasOperaciones = new List<string>(operacionesExist) { nuevaOperacion };

                if (OperacionDirectaEsValida(nuevosNumeros, nuevasOperaciones))
                {
                    return numero;
                }
            }

            // Si falla, intentar con el método normal (pero asegurando minDig)
            for (int intento = 0; intento < 50; intento++)
            {
                long numero = GenerarNumeroNormal(minDig, maxDig, valorMaximo, digitosPermitidos, random);

                var nuevosNumeros = new List<long>(numerosExist) { numero };
                var nuevasOperaciones = new List<string>(operacionesExist) { nuevaOperacion };

                if (OperacionDirectaEsValida(nuevosNumeros, nuevasOperaciones))
                {
                    return numero;
                }
            }

            // Como último recurso, usar números seguros
            if (nuevaOperacion == "+")
            {
                return GenerarNumeroSeguroSuma(digitosPermitidos, minDig, random);
            }
            else
            {
                return GenerarNumeroSeguroResta(numerosExist.Last(), minDig, random);
            }
        }

        private long GenerarNumeroSeguroSuma(string[] digitosPermitidos, int minDig, Random random)
        {
            // Generar número con exactamente minDig dígitos, todos entre 1-4
            string numeroStr = "";

            // Primer dígito (1-4)
            numeroStr += random.Next(1, 5).ToString();

            // Resto de dígitos (0-4)
            for (int i = 1; i < minDig; i++)
            {
                numeroStr += random.Next(0, 5).ToString();
            }

            return long.Parse(numeroStr);
        }

        private long GenerarNumeroSeguroResta(long ultimoNumero, int minDig, Random random)
        {
            // Asegurarnos de no restar más de lo posible
            long maxResta = Math.Min(ultimoNumero - 1, (long)Math.Pow(10, minDig) - 1);

            if (maxResta <= 0) return 1;

            // Generar número con exactamente minDig dígitos
            long numero = (long)Math.Pow(10, minDig - 1) + random.Next(0, (int)Math.Pow(10, minDig - 1));
            return Math.Min(numero, maxResta);
        }

        private long GenerarNumeroConLongitud(string[] digitosPermitidos, int longitudDeseada, int maxDig, long valorMaximo, Random random)
        {
            int longitud = Math.Min(longitudDeseada, maxDig);
            string numeroStr = "";

            // Primer dígito no puede ser cero y priorizamos dígitos que funcionen con FingerMath
            var primerosDigitos = digitosPermitidos.Where(d => d != "0")
                                                  .OrderBy(d => Math.Abs(int.Parse(d) - 3)) // Prioriza dígitos cercanos a 3
                                                  .ToArray();

            if (!primerosDigitos.Any()) primerosDigitos = new[] { "1" };
            numeroStr += primerosDigitos[random.Next(Math.Min(3, primerosDigitos.Length))]; // Elige entre los 3 más cercanos a 3

            // Resto de dígitos - priorizando valores que no compliquen FingerMath
            for (int i = 1; i < longitud; i++)
            {
                var digitosPosibles = digitosPermitidos.OrderBy(d =>
                   Math.Abs(int.Parse(d) - 2)).ToArray(); // Prioriza dígitos cercanos a 2
                numeroStr += digitosPosibles[random.Next(Math.Min(3, digitosPosibles.Length))];
            }

            long numero = long.Parse(numeroStr);

            // Ajustar según valor máximo
            if (valorMaximo > 0 && numero > valorMaximo)
            {
                string maxStr = valorMaximo.ToString();
                if (numeroStr.Length > maxStr.Length)
                {
                    numeroStr = numeroStr.Substring(0, maxStr.Length);
                    numero = long.Parse(numeroStr);
                }
            }

            return numero;
        }

        private long GenerarNumeroFingerMath(int minDig, int maxDig, long valorMaximo, string[] digitosPermitidos,
                           List<long> numerosExist, List<string> operacionesExist,
                           string nuevaOperacion, Random random)
        {
            int columnas = maxDig;
            string numeroStr = "";
            bool[] pulgarUsadoPorColumna = new bool[columnas];
            int[] acumuladoPorColumna = new int[columnas];

            // Calcular estado actual de cada columna
            for (int col = 0; col < columnas; col++)
            {
                foreach (var (num, op) in numerosExist.Zip(operacionesExist, (n, o) => (n, o)))
                {
                    string numStr = num.ToString().PadLeft(columnas, '0');
                    int digito = int.Parse(numStr[numStr.Length - 1 - col].ToString());

                    if (op == "+")
                    {
                        if (digito >= 5)
                        {
                            pulgarUsadoPorColumna[col] = true;
                            digito -= 5;
                        }
                        acumuladoPorColumna[col] += digito;
                    }
                    else
                    {
                        if (digito >= 5)
                        {
                            pulgarUsadoPorColumna[col] = false;
                            digito -= 5;
                        }
                        acumuladoPorColumna[col] -= digito;
                    }
                }
            }

            // Generar cada dígito considerando las restricciones de su columna
            for (int col = 0; col < columnas; col++)
            {
                // Obtener dígitos posibles ordenados de menor a mayor (priorizando dígitos bajos)
                var digitosPosibles = digitosPermitidos.Select(d => int.Parse(d))
                    .Where(d => {
                        bool usarPulgar = d >= 5;
                        int valorDigito = usarPulgar ? d - 5 : d;

                        if (nuevaOperacion == "+")
                        {
                            if (usarPulgar && pulgarUsadoPorColumna[col]) return false;

                            int nuevoAcumulado = acumuladoPorColumna[col] + valorDigito;
                            bool nuevoPulgar = pulgarUsadoPorColumna[col] || usarPulgar;

                            if (nuevoPulgar)
                                return nuevoAcumulado <= 4; // Máximo 9 (5+4)
                            else
                                return nuevoAcumulado <= 4; // Máximo 4
                        }
                        else
                        {
                            if (usarPulgar && !pulgarUsadoPorColumna[col]) return false;

                            int nuevoAcumulado = acumuladoPorColumna[col] - valorDigito;
                            bool nuevoPulgar = pulgarUsadoPorColumna[col] && !usarPulgar;

                            return nuevoAcumulado >= 0; // No puede quedar negativo
                        }
                    })
                    .OrderBy(d => d) // Priorizar dígitos más pequeños
                    .ToList();

                if (!digitosPosibles.Any())
                {
                    // Si no hay dígitos posibles, usar el más seguro posible
                    if (nuevaOperacion == "+")
                        digitosPosibles = new List<int> { 1, 2, 3, 4 };
                    else
                        digitosPosibles = new List<int> { 1, 2, 3, 4 };
                }

                // Elegir aleatoriamente, pero con mayor probabilidad para dígitos bajos
                int index;
                if (digitosPosibles.Count > 3)
                {
                    // Usar distribución triangular para favorecer números bajos
                    double r = random.NextDouble();
                    index = (int)Math.Floor(digitosPosibles.Count * (1 - Math.Sqrt(1 - r)));
                }
                else
                {
                    index = random.Next(digitosPosibles.Count);
                }

                int digitoSeleccionado = digitosPosibles[index];
                numeroStr = digitoSeleccionado.ToString() + numeroStr;
            }

            // Asegurar mínimo de dígitos y valor máximo
            while (numeroStr.Length < minDig)
                numeroStr = "1" + numeroStr;

            if (numeroStr.Length > maxDig)
                numeroStr = numeroStr.Substring(0, maxDig);

            long numero = long.Parse(numeroStr);

            if (valorMaximo > 0 && numero > valorMaximo)
                return GenerarNumeroFingerMath(minDig, maxDig, valorMaximo, digitosPermitidos,
                                             numerosExist, operacionesExist, nuevaOperacion, random);

            return numero;
        }

        private bool OperacionDirectaEsValida(List<long> numeros, List<string> operaciones)
        {
            int maxLength = numeros.Max(n => n.ToString().Length);
            var numerosStr = numeros.Select(n => n.ToString().PadLeft(maxLength, '0')).ToList();

            // Primero verificar que el resultado total no sea negativo (para restas)
            long resultadoTotal = CalcularResultado(numeros, operaciones);
            if (resultadoTotal < 0) return false;

            // Validar cada columna individualmente
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
                        // Reglas para suma
                        if (digito >= 5)
                        {
                            if (pulgarAbajo) return false; // No se puede sumar otro pulgar
                            pulgarAbajo = true;
                            digito -= 5;
                        }

                        acumulado += digito;

                        // Validar límites
                        if (pulgarAbajo)
                        {
                            if (acumulado > 4) return false; // Máximo 9 (5+4)
                        }
                        else
                        {
                            if (acumulado > 4) return false; // Máximo 4 dedos
                        }
                    }
                    else
                    {
                        // Reglas para resta
                        if (digito >= 5)
                        {
                            if (!pulgarAbajo) return false; // No se puede restar pulgar si no está
                            pulgarAbajo = false;
                            digito -= 5;
                        }

                        acumulado -= digito;
                        if (acumulado < 0) return false; // No puede quedar negativo

                        // Validar límites después de restar
                        if (pulgarAbajo)
                        {
                            if (acumulado > 4) return false;
                        }
                        else
                        {
                            if (acumulado > 4) return false;
                        }
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