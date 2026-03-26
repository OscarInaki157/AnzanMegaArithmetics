using AnzanMegaArithmetics.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Text.Json;

namespace AnzanMegaArithmetics.Controllers
{
    [Authorize]
    public class HojasEjerciciosController : Controller
    {
        //Tablas de multiplicar

        [HttpGet]
        public IActionResult ConfigurarHojasEjerciciosTablas()
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
                TipoPregunta = "1",
                ParImpar = "ambos",
                DigitosMultiplicacion = "1,2,3,4,5,6,7,8,9"
            };
        }

        [HttpPost]
        public IActionResult HojaEjerciciosTablas(ConfTablasModel config) 
        {
            int total = config.CantidadEjercicios;
            List<RMultiplicacionModel> resultados = new List<RMultiplicacionModel>();
            int longiudMultiplicando = int.Parse(config.TipoPregunta);
            var digitosPermitidos = config.DigitosMultiplicacion.Split(',').Select(d => d.Trim()).Where(d => d.Length == 1).ToArray();
            var parImpar = config.ParImpar;
            Random rand = new Random();

            for (int i = 1; i <= total; i++)
            {
                int multiplicando = GenerarNumeroAleatorioT(longiudMultiplicando, rand);
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

                int multiplicador = posiblesMultiplicadores[rand.Next(posiblesMultiplicadores.Count)];

                int resultado = multiplicando * multiplicador;

                resultados.Add(new RMultiplicacionModel
                {
                    Ejercicio = i,
                    Multiplicando = multiplicando,
                    Multiplicador = multiplicador,
                    RespuestaUsuario = 0,
                    TiempoRespuesta = 0.0
                });

            }

            return View(resultados);

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


        //Tablas de multiplicar
        //Multiplicacion

        [HttpGet]
        public IActionResult ConfigurarHojasEjerciciosMultiplicacion()
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
                    DigitosMultiplicando = "2",
                    DigitosMultiplicador = "2",
                    TiempoMeditacion = 3
                };
            }

            return View(modelo);
        }

        [HttpPost]
        public IActionResult HojaEjerciciosMultiplicacion(ConfMultiModel config) 
        {
            int total = config.CantidadEjercicios;
            List<RMultiplicationModel> resultados = new List<RMultiplicationModel>();
            Random rand = new Random();

            for (int i = 1; i <= total; i++)
            {
                int multiplicando = GenerarNumero(rand, config.DigitosMultiplicando);
                int multiplicador = GenerarNumero(rand, config.DigitosMultiplicador);

                resultados.Add(new RMultiplicationModel
                {
                    Multiplicando = multiplicando,
                    Multiplicador = multiplicador,
                    RespuestaUsuario = 0,
                    TiempoRespuesta = 0.0
                });
            }
            ViewBag.FormatoPregunta = config.FormatoPregunta;
            return View(resultados);
        }

        private int GenerarNumero(Random rng, string digitosPermitidos)
        {
            var digitos = digitosPermitidos.Split(',').Select(int.Parse).ToList();
            int d = digitos[rng.Next(digitos.Count)];
            int min = (int)Math.Pow(10, d - 1);
            int max = (int)Math.Pow(10, d) - 1;
            return rng.Next(min, max + 1);
        }
        //Multiplicacion
        //raices cuadradas
        [HttpGet]
        public IActionResult ConfigurarHojasEjerciciosRaices()
        {
            var userId = HttpContext.Session.GetInt32("Id_Usuario");

            if (userId == null || userId == 0)
            {
                return RedirectToAction("Inicio", "Inicio");
            }

            ConfRaicesModel config;
            var configJson = HttpContext.Session.GetString("RaicesConf");

            if (string.IsNullOrEmpty(configJson))
            {

                config = new ConfRaicesModel
                {
                    CantidadEjercicios = 5,
                    TipoEjercicio = "2digitos"
                };

                HttpContext.Session.SetString("RaicesConf", JsonSerializer.Serialize(config));

            }
            else
            {
                config = JsonSerializer.Deserialize<ConfRaicesModel>(configJson);
            }

            return View(config);
        }
        [HttpPost]
        public IActionResult HojaEjerciciosRaices(ConfRaicesModel config)
        {
            List<EjercicioRaicesModel> ejercicios = GenerarEjerciciosRaices(config);
            return View(ejercicios);
        }

        private List<EjercicioRaicesModel> GenerarEjerciciosRaices(ConfRaicesModel config)
        {
            List<EjercicioRaicesModel> ejercicios = new();
            Random rand = new Random();

            for (int i = 0; i < config.CantidadEjercicios; i++)
            {
                EjercicioRaicesModel ejercicio = new EjercicioRaicesModel
                {
                    Id_Ejercicio = i + 1
                };

                if (config.TipoEjercicio.Contains("2"))
                {
                    int raiz = rand.Next(10, 99);
                    ejercicio.Numero_Base = raiz * raiz;
                }
                else if (config.TipoEjercicio.Contains("3"))
                {
                    int raiz = rand.Next(100, 999);
                    ejercicio.Numero_Base = raiz * raiz;
                }

                ejercicios.Add(ejercicio);

            }

            return ejercicios;
        }

        //raices cuadradas
        //potencias
        [HttpGet]
        public IActionResult ConfigurarHojasEjerciciosPotencias()
        {
            var userId = HttpContext.Session.GetInt32("Id_Usuario");

            if (userId == null || userId == 0)
            {
                return RedirectToAction("Inicio", "Inicio");
            }

            ConfPotenciasModel config;
            var configJson = HttpContext.Session.GetString("ConfPotencias");

            if (string.IsNullOrEmpty(configJson))
            {

                config = new ConfPotenciasModel
                {
                    CantidadEjercicios = 10,
                    NumeroInicial = 11,
                    NumeroFinal = 19
                };

                HttpContext.Session.SetString("ConfPotencias", JsonSerializer.Serialize(config));
            }
            else
            {
                config = JsonSerializer.Deserialize<ConfPotenciasModel>(configJson);
            }

            return View(config);
        }
        [HttpPost]
        public IActionResult HojaEjerciciosPotencias(ConfPotenciasModel config)
        {
            List<EjercicioPotenciasModel> ejercicios = GenerarEjerciciosPotencias(config);
            return View(ejercicios);
        }
        private List<EjercicioPotenciasModel> GenerarEjerciciosPotencias(ConfPotenciasModel config)
        {
            List<EjercicioPotenciasModel> ejercicios = new();
            Random rand = new Random();

            for (int i = 0; i < config.CantidadEjercicios; i++)
            {
                int numeroBase = rand.Next(config.NumeroInicial, config.NumeroFinal + 1);

                EjercicioPotenciasModel ejercicio = new EjercicioPotenciasModel
                {
                    Id_Ejercicio = i + 1,
                    Numero_Base = numeroBase
                };
                ejercicios.Add(ejercicio);
            }

            return ejercicios;
        }
        //potencias
        //división
        [HttpGet]
        public IActionResult ConfigurarHojasEjerciciosDivision() 
        {
            var userId = HttpContext.Session.GetInt32("Id_Usuario");

            if (userId == null || userId == 0)
            {
                return RedirectToAction("Inicio", "Inicio");
            }

            var configJson = HttpContext.Session.GetString("UltimaConfigDivision");
            ConfDivModel modelo;

            if (!string.IsNullOrEmpty(configJson))
            {
                modelo = System.Text.Json.JsonSerializer.Deserialize<ConfDivModel>(configJson);
            }
            else
            {
                modelo = new ConfDivModel
                {
                    CantidadEjercicios = 5,
                    FormatoPregunta = "galera",
                    DireccionRespuesta = "IzquierdaADerecha",
                    VelocidadPreguntas = "0.0",
                    DigitosDividendo = "2",
                    DigitosDivisor = "2",
                    TiempoMeditacion = 3,
                    TipoEjercicio = "exacta"
                };
            }

            return View(modelo);
        }

        [HttpPost]
        public IActionResult HojaEjerciciosDivision(ConfDivModel config) 
        {
            List<RDivisionModel> listaEjercicios = GenerarEjercicios(config);
            ViewBag.FormatoPregunta = config.FormatoPregunta;
            return View(listaEjercicios);
        }

        private List<RDivisionModel> GenerarEjercicios(ConfDivModel config)
        {
            var lista = new List<RDivisionModel>();
            var random = new Random();

            int digitosDividendo = int.Parse(config.DigitosDividendo);
            int digitosDivisor = int.Parse(config.DigitosDivisor);

            int minDiv = (int)Math.Pow(10, digitosDividendo - 1);
            int maxDiv = (int)Math.Pow(10, digitosDividendo) - 1;

            int minDvr = (int)Math.Pow(10, digitosDivisor - 1);
            int maxDvr = (int)Math.Pow(10, digitosDivisor) - 1;

            for (int i = 0; i < config.CantidadEjercicios; i++)
            {
               
                int divisor = random.Next(minDvr, maxDvr + 1);
                int dividendo = random.Next(minDiv, maxDiv + 1);

                if (config.TipoEjercicio == "exacta" && maxDiv >= divisor)
                {
                    int cociente = dividendo / divisor;
                    if (cociente == 0) cociente = 1;

                    int nuevoDividendo = divisor * cociente;

                   
                    while (nuevoDividendo > maxDiv && nuevoDividendo > divisor)
                    {
                        nuevoDividendo -= divisor;
                    }

                    
                    if (nuevoDividendo < minDiv)
                    {
                        nuevoDividendo += divisor;
                    }

                    dividendo = nuevoDividendo;
                }

                
                if (dividendo == 0) dividendo = minDiv;
                if (divisor == 0) divisor = minDvr;

                lista.Add(new RDivisionModel
                {
                    Dividendo = dividendo,
                    Divisor = divisor,
                    RespuestaUsuario = -1
                });
            }
            return lista;
        }
        //división
        //sumaresta
        [HttpGet]
        public IActionResult ConfigurarHojasEjerciciosSR()
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
                    CantidadEjercicios = 5,
                    DigitosSuma = "1,2,3,4,5,6,7,8,9",
                    DigitosResta = "1,2,3,4,5,6,7,8,9",
                    NumeroOperaciones = 4,
                    TiempoMeditacion = 3
                };
            }

            ModelState.Clear();
            return View(config);
        }

        [HttpPost]
        public IActionResult HojaEjerciciosSR(ConfSumaRestaModel config)
        {
            // Guardamos la configuración
            HttpContext.Session.SetString("UltimaConfigSR", JsonSerializer.Serialize(config));

            // Aseguramos que al menos pida 1 ejercicio
            config.CantidadEjercicios = Math.Max(1, config.CantidadEjercicios);

            var random = new Random();
            var listaEjercicios = new List<EjerciciosSumaRestaHoja>();

            // Bucle para generar todos los ejercicios que pidió (ej. 10, 20 o 50)
            for (int e = 0; e < config.CantidadEjercicios; e++)
            {
                // Por cada vuelta, llamamos al método privado de abajo
                var ejercicio = GenerarUnEjercicioParaHoja(config, random);
                listaEjercicios.Add(ejercicio);
            }

            // Devolvemos la vista pasándole TODA la lista
            return View(listaEjercicios);
        }

   
        private EjerciciosSumaRestaHoja GenerarUnEjercicioParaHoja(ConfSumaRestaModel config, Random random)
        {
            int numOperaciones = config.NumeroOperaciones;
            int minDig = config.MinDigitos;
            int maxDig = config.MaxDigitos;
            long valorMaximo = config.ValorMaximo;
            bool usarMax = config.UsarMaximoComoBase;
            string tipoOperacion = config.TipoOperacion ?? "suma";

            string[] sumaPermitidos = (config.DigitosSuma ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
            string[] restaPermitidos = (config.DigitosResta ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();

            bool directaSuma = config.DirectaSuma;
            bool directaResta = config.DirectaResta;
            bool operacionesDirectas = directaSuma || directaResta || (tipoOperacion == "ambos" && directaSuma && directaResta);
            bool esSoloResta = tipoOperacion == "resta";

            var numeros = new List<long>();
            var operaciones = new List<string>();

            long anchorValue = 0;
            int posAncla = -1;

            if (valorMaximo > 0)
            {
                double varianza = 0.55 + (random.NextDouble() * 0.10);
                anchorValue = (long)(valorMaximo * varianza);

                long limiteMaxPorDigitos = (long)Math.Pow(10, maxDig) - 1;
                long limiteMinPorDigitos = minDig > 1 ? (long)Math.Pow(10, minDig - 1) : 1;

                if (anchorValue > limiteMaxPorDigitos) anchorValue = limiteMaxPorDigitos;
                if (anchorValue < limiteMinPorDigitos) anchorValue = limiteMinPorDigitos;

                posAncla = usarMax ? 0 : random.Next(0, numOperaciones);
            }

            for (int intento = 0; intento < 300; intento++)
            {
                numeros.Clear();
                operaciones.Clear();
                long acumulado = 0;
                bool intentoValido = true;

                for (int i = 0; i < numOperaciones; i++)
                {
                    string op = "+";
                    if (i > 0)
                    {
                        if (tipoOperacion == "suma") op = "+";
                        else if (tipoOperacion == "resta") op = "-";
                        else op = random.Next(0, 2) == 0 ? "+" : "-";
                    }

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

                    if (valorMaximo > 0 && i == posAncla)
                    {
                        nuevoNumero = anchorValue;
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
                    else
                    {
                        long topeMaximo = 0;
                        if (valorMaximo > 0)
                        {
                            topeMaximo = op == "+" ? valorMaximo - acumulado : acumulado;
                            long valorMinimoPosible = minDig > 1 ? (long)Math.Pow(10, minDig - 1) : 1;
                            if (topeMaximo < valorMinimoPosible) { intentoValido = false; break; }
                        }

                        long maxParaGenerar = valorMaximo > 0 ? topeMaximo : 0;

                        if (i == 0 && valorMaximo == 0 && (tipoOperacion == "resta" || tipoOperacion == "ambos"))
                        {
                            long maxPosibleResta = (long)Math.Pow(10, maxDig) - 1;
                            long restaEstimada = maxPosibleResta * numOperaciones;

                            nuevoNumero = GenerarNumeroNormal(minDig, Math.Min(maxDig + 2, 10), 0, digitosParaGenerar, random);

                            if (nuevoNumero <= restaEstimada)
                            {
                                nuevoNumero += restaEstimada + random.Next(1, 1000);
                            }
                        }
                        else
                        {
                            if (operacionesDirectas && i > 0)
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
                        return new EjerciciosSumaRestaHoja
                        {
                            Numeros = numeros,
                            Operaciones = operaciones,
                            RespuestaCorrecta = acumulado
                        };
                    }
                }
            }

            return new EjerciciosSumaRestaHoja
            {
                Numeros = numeros,
                Operaciones = operaciones,
                RespuestaCorrecta = CalcularResultado(numeros, operaciones)
            };
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
        //sumaresta
        //cuadros
        [HttpGet]
        public IActionResult ConfigurarHojasEjerciciosCuadros(ConfCuadrosModel config)
        {
            var userId = HttpContext.Session.GetInt32("Id_Usuario");

            if (userId == null || userId == 0)
            {
                return RedirectToAction("Inicio", "Inicio");
            }

                config = new ConfCuadrosModel
                {
                    CantidadRejillas = 1,
                    DimensionRejilla = "10X5",
                    TipoIluminacion = "ninguna",
                    TipoOperacion = "suma",
                    DigitosSuma = "1,2,3,4,5,6,7,8,9",
                    DigitosResta = "1,2,3,4,5,6,7,8,9"
                };

            return View(config);
        }

        [HttpPost]
        public IActionResult HojaEjerciciosCuadros(ConfCuadrosModel config)
        {
            List<EjercicioCuadrosModel> sesionejercicios = GenerarRejillas(config);
            ViewBag.Dimension = config.DimensionRejilla;
            return View(sesionejercicios);
        }

        private List<EjercicioCuadrosModel> GenerarRejillas(ConfCuadrosModel config)
        {
            string dgSuma = config.DigitosSuma;
            string dgResta = config.DigitosResta;
            string[] dimension = config.DimensionRejilla.Split('X');
            string filasStr = dimension[0].Trim();
            string columnasStr = dimension[1].Trim();

            int filas = int.Parse(filasStr);
            int columnas = int.Parse(columnasStr);
            int profundidad = filas * columnas;
            List<int> DigitosEjercicio = new();
            Random random = new Random();

            //sacar los digitos
            if (config.TipoOperacion == "suma")
            {
                //solo se pasan digitos suma
                List<int> DigitosSuma = ParsearDigitos(dgSuma);

                DigitosEjercicio = DigitosSuma.ToList();
            }
            else
            {
                //pasan digitos suma y resta
                List<int> DigitosSuma = ParsearDigitos(dgSuma);
                List<int> DigitosResta = ParsearDigitos(dgResta);
                //convertir los digitos a negativos, para resta

                List<int> Negativos = DigitosResta.Select(d => -d).ToList();

                DigitosEjercicio = DigitosSuma.Concat(Negativos).ToList();
            }

            if (!DigitosEjercicio.Any())
            {
                // Usar valores por defecto si no hay dígitos
                DigitosEjercicio = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            }

            List<EjercicioCuadrosModel> ejercicios = new();

            for (int i = 0; i < config.CantidadRejillas; i++)
            {
                EjercicioCuadrosModel ejercicio = new();
                ejercicio.NumeroEjercicio = i + 1;
                ejercicio.Iluminacion = config.TipoIluminacion;

                ejercicio.Rejilla = GenerarEjercicioCuadros(profundidad, DigitosEjercicio, random);


                ejercicios.Add(ejercicio);
            }

            return ejercicios;
        }

        private List<int> ParsearDigitos(string digitos)
        {
            return digitos.Split(',').Select(d => d.Trim()).Where(d => !string.IsNullOrEmpty(d)).Select(int.Parse).ToList();
        }

        private List<int> GenerarEjercicioCuadros(int profundidad, List<int> DigitosEjercicio, Random random)
        {
            List<int> rejilla = new();
            int sumaTotal = 0;

            // Primero llenar con números que nos ayuden a mantener suma positiva
            for (int i = 0; i < profundidad; i++)
            {
                int numero;
                int intentos = 0;

                do
                {
                    if (sumaTotal < 0 && intentos > 10)
                    {
                        // Filtrar solo números positivos
                        var positivos = DigitosEjercicio.Where(n => n > 0).ToList();
                        if (positivos.Any())
                        {
                            numero = positivos[random.Next(positivos.Count)];
                        }
                        else
                        {
                            numero = DigitosEjercicio[random.Next(DigitosEjercicio.Count)];
                        }
                    }
                    else
                    {
                        numero = DigitosEjercicio[random.Next(DigitosEjercicio.Count)];
                    }

                    intentos++;

                } while ((sumaTotal + numero) < 0 && intentos < 20);

                rejilla.Add(numero);
                sumaTotal += numero;
            }

            // Verificación final por si acaso
            if (sumaTotal < 0)
            {
                // Reemplazar algunos negativos por positivos
                for (int i = 0; i < rejilla.Count && sumaTotal < 0; i++)
                {
                    if (rejilla[i] < 0)
                    {
                        var positivos = DigitosEjercicio.Where(n => n > 0).ToList();
                        if (positivos.Any())
                        {
                            int nuevoNumero = positivos[random.Next(positivos.Count)];
                            sumaTotal -= rejilla[i]; // quitar el negativo
                            sumaTotal += nuevoNumero; // agregar el positivo
                            rejilla[i] = nuevoNumero;
                        }
                    }
                }
            }

            return rejilla;
        }


        //cuadros
    }
}
