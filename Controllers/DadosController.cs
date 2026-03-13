using AnzanMegaArithmetics.Models;
using AnzanMegaArithmetics.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Text.Json;

namespace AnzanMegaArithmetics.Controllers
{
    [Authorize]
    public class DadosController : Controller
    {
        private readonly IPruebasDBService _pruebasDBService;
        public DadosController(IPruebasDBService pruebasDBService)
        {
            this._pruebasDBService = pruebasDBService;
        }

        [HttpGet]
        public IActionResult LimpiarYDashboard()
        {
            HttpContext.Session.Remove("DadosEjercicios");
            HttpContext.Session.Remove("DadosRespuestas");
            //HttpContext.Session.Remove("DadosConf");
            HttpContext.Session.Remove("EjercicioActualDados");
            HttpContext.Session.Remove("DadosTiempoRestante");
            HttpContext.Session.Remove("DadosInicioTiempo");
            return RedirectToAction("Desafios", "Dashboard");
        }

        [HttpGet]
        public IActionResult FormularioDados()
        {
            var userId = HttpContext.Session.GetInt32("Id_Usuario");

            if (userId == null || userId == 0)
            {
                return RedirectToAction("Inicio", "Inicio");
            }

            ConfDadosModel config;
            var configJson = HttpContext.Session.GetString("DadosConf");

            if (string.IsNullOrEmpty(configJson))
            {
                config = new ConfDadosModel
                {
                    ModoJuego = "Aritmetica",
                    CantidadEjercicios = 20,
                    TiempoTotal = 3,
                    TiempoMeditacion = 3,
                    TipoPrueba = "Matemáticas con Dados tradicional",
                    UsaJerarquia = false,
                    MultiplicadorLibre = 7
                };
            }
            else
            {
                config = JsonSerializer.Deserialize<ConfDadosModel>(configJson);
            }

            ViewBag.ModoActivo = config.ModoJuego;

            return View(config);
        }

        [HttpPost]
        public IActionResult ConcentracionDados(ConfDadosModel config)
        {
            try
            {
                //generar ejercicios
                List<EjercicioDadosModel> ejercicios = GenerarEjerciciosDados(config);

                //generar respuestas
                List<RespuestaDadosModel> respuestas = ejercicios.Select(e => new RespuestaDadosModel
                {
                    Id_Ejercicio = e.Id_Ejercicio,
                    Respuesta_Usuario = string.Empty,
                    Es_Correcta = false,
                    Tiempo_Respuesta = 0,
                    DadosUtilizados = 0
                }).ToList();

                //guardar en session
                HttpContext.Session.SetString("DadosConf", JsonSerializer.Serialize(config));
                HttpContext.Session.SetString("DadosEjercicios", JsonSerializer.Serialize(ejercicios));
                HttpContext.Session.SetString("DadosRespuestas", JsonSerializer.Serialize(respuestas));
                HttpContext.Session.SetInt32("EjercicioActualDados", 1);

                HttpContext.Session.SetInt32("DadosTiempoRestante", config.TiempoTotal * 60);
                HttpContext.Session.SetString("DadosInicioTiempo", DateTime.Now.ToString());

                ViewBag.TiempoMeditacion = config.TiempoMeditacion;
                return View();
            }
            catch (Exception ex)
            {
                return RedirectToAction("FormularioDados");
            }
        }

        private List<EjercicioDadosModel> GenerarEjerciciosDados(ConfDadosModel config)
        {
            List<EjercicioDadosModel> ejercicios = new();
            Random _random = new Random();

            int minMult = 2, maxMult = 11;
            bool esLibre = config.RangoDados == "Libre";

            if (config.RangoDados == "9-16") { minMult = 9; maxMult = 17; }
            else if (config.RangoDados == "15-24") { minMult = 15; maxMult = 24; }

            for (int i = 0; i < config.CantidadEjercicios; i++)
            {
                int[] dados;
                int resultado;

                switch (config.ModoJuego)
                {
                    case "MultiSuma":
                        int cantidadASumarMulti = config.NumeroDados > 0 ? config.NumeroDados : 5;
                        dados = new int[cantidadASumarMulti + 1];
                        int sumaParcial = 0;
                        for (int j = 0; j < cantidadASumarMulti; j++)
                        {
                            dados[j] = _random.Next(1, 7);
                            sumaParcial += dados[j];
                        }
                        dados[cantidadASumarMulti] = esLibre ? config.MultiplicadorLibre : _random.Next(minMult, maxMult);
                        resultado = sumaParcial * dados[cantidadASumarMulti];
                        break;

                    case "SumaFlash":
                        int cantidadASumar = config.NumeroDados > 0 ? config.NumeroDados : 5;
                        dados = new int[cantidadASumar];
                        for (int j = 0; j < cantidadASumar; j++)
                        {
                            dados[j] = _random.Next(1, 7);
                        }
                        resultado = dados.Sum();
                        break;

                    default:

                        resultado = _random.Next(1, 13);
                        dados = GenerarDadosConSolucion(resultado);
                        break;
                }

                ejercicios.Add(new EjercicioDadosModel
                {
                    Id_Ejercicio = i + 1,
                    Dados = dados,
                    Dado_Resultado = resultado,
                    SolucionesPosibles = new List<string>() // Opcional para el modo tradicional
                });
            }

            return ejercicios;
        }

        private int[] GenerarDadosConSolucion(int resultado)
        {
            int[] dados = new int[5];
            Random _random = new Random();

            List<int> factores = ObtenerFactores(resultado);
            if (factores.Count > 0)
            {
                dados[0] = factores[_random.Next(factores.Count)];
                dados[1] = resultado / dados[0];
            }
            else
            {
                dados[0] = _random.Next(1, 7);
                dados[1] = resultado + dados[0] <= 6 ? resultado + dados[0] :
                           dados[0] - resultado >= 1 ? dados[0] - resultado : _random.Next(1, 7);
            }

            for (int i = 2; i < 5; i++)
            {
                dados[i] = _random.Next(1, 7);
            }

            return dados;
        }

        private List<int> ObtenerFactores(int numero)
        {
            var factores = new List<int>();
            for (int i = 1; i <= 6 && i <= numero; i++)
            {
                if (numero % i == 0 && numero / i <= 6)
                {
                    factores.Add(i);
                }
            }
            return factores;
        }

        [HttpGet]
        public IActionResult EjercicioDados()
        {
            try
            {
                var configJson = HttpContext.Session.GetString("DadosConf");
                var ejerciciosJson = HttpContext.Session.GetString("DadosEjercicios");

                if (string.IsNullOrEmpty(configJson) || string.IsNullOrEmpty(ejerciciosJson))
                {
                    return RedirectToAction("FormularioDados");
                }

                List<EjercicioDadosModel> ejercicios = JsonSerializer.Deserialize<List<EjercicioDadosModel>>(ejerciciosJson);
                ConfDadosModel config = JsonSerializer.Deserialize<ConfDadosModel>(configJson);
                int ejercicioActual = HttpContext.Session.GetInt32("EjercicioActualDados") ?? 1;

                if (ejercicioActual > ejercicios.Count)
                {
                    return RedirectToAction("ResultadosDados");
                }

                int tiempoRestante = HttpContext.Session.GetInt32("DadosTiempoRestante") ?? 0;

                if (config.TiempoTotal > 0 && tiempoRestante <= 0)
                {
                    return RedirectToAction("ResultadosDados");
                }

                EjercicioDadosModel ejercicio = ejercicios.FirstOrDefault(e => e.Id_Ejercicio == ejercicioActual);
                if (ejercicio == null)
                {
                    return RedirectToAction("FormularioDados");
                }

                ViewBag.EjercicioActual = ejercicioActual;
                ViewBag.TotalEjercicios = ejercicios.Count();
                ViewBag.TiempoRestante = tiempoRestante;
                return View(ejercicio);
            }
            catch (Exception ex)
            {
                return RedirectToAction("FormularioDados");
            }
        }

        [HttpPost]
        public IActionResult EjercicioDados(string respuestaUsuario, double tiempoRespuesta)
        {
            try
            {
                var ejerciciosJson = HttpContext.Session.GetString("DadosEjercicios");
                var respuestasJson = HttpContext.Session.GetString("DadosRespuestas");
                var ejercicioActual = HttpContext.Session.GetInt32("EjercicioActualDados") ?? 1;
                var tiempoRestante = HttpContext.Session.GetInt32("DadosTiempoRestante") ?? 0;
                var configJson = HttpContext.Session.GetString("DadosConf");
                var config = JsonSerializer.Deserialize<ConfDadosModel>(configJson);

                if (string.IsNullOrEmpty(ejerciciosJson) || string.IsNullOrEmpty(respuestasJson))
                {
                    return RedirectToAction("FormularioDados");
                }

                List<EjercicioDadosModel> ejercicios = JsonSerializer.Deserialize<List<EjercicioDadosModel>>(ejerciciosJson);
                List<RespuestaDadosModel> respuestas = JsonSerializer.Deserialize<List<RespuestaDadosModel>>(respuestasJson);

                var ejercicio = ejercicios.FirstOrDefault(e => e.Id_Ejercicio == ejercicioActual);
                var respuesta = respuestas.FirstOrDefault(r => r.Id_Ejercicio == ejercicioActual);

                if (ejercicio == null || respuesta == null)
                {
                    return RedirectToAction("FormularioDados");
                }

                respuesta.Respuesta_Usuario = respuestaUsuario ?? string.Empty;
                respuesta.Tiempo_Respuesta = tiempoRespuesta;

                respuesta.Es_Correcta = ValidarRespuestaMatematica(respuesta.Respuesta_Usuario, ejercicio.Dados, ejercicio.Dado_Resultado, config.UsaJerarquia);
                respuesta.DadosUtilizados = ContarDadosUtilizados(respuesta.Respuesta_Usuario, ejercicio.Dados);

                HttpContext.Session.SetString("DadosRespuestas", JsonSerializer.Serialize(respuestas));

                if (config.TiempoTotal > 0 && tiempoRestante > 0)
                {
                    tiempoRestante = Math.Max(0, tiempoRestante - (int)Math.Ceiling(tiempoRespuesta));
                }

                HttpContext.Session.SetInt32("DadosTiempoRestante", tiempoRestante);
                HttpContext.Session.SetInt32("EjercicioActualDados", ejercicioActual + 1);

                if (ejercicioActual + 1 > ejercicios.Count || (config.TiempoTotal > 0 && tiempoRestante <= 0))
                {
                    return RedirectToAction("ResultadosDados");
                }
                else
                {
                    return RedirectToAction("EjercicioDados");
                }
            }
            catch (Exception ex)
            {
                return RedirectToAction("FormularioDados");
            }
        }

        [HttpGet]
        public IActionResult ResultadosDados() 
        {
            try 
            {
                var userId = HttpContext.Session.GetInt32("Id_Usuario");
                if (userId == null || userId == 0)
                {
                    return RedirectToAction("Inicio", "Inicio");
                }

                var ejerciciosJson = HttpContext.Session.GetString("DadosEjercicios");
                var respuestasJson = HttpContext.Session.GetString("DadosRespuestas");
                var configJson = HttpContext.Session.GetString("DadosConf");

                if (string.IsNullOrEmpty(ejerciciosJson) || string.IsNullOrEmpty(respuestasJson) || string.IsNullOrEmpty(configJson))
                {
                    return RedirectToAction("FormularioDados");
                }

                var ejercicios = JsonSerializer.Deserialize<List<EjercicioDadosModel>>(ejerciciosJson);
                var respuestas = JsonSerializer.Deserialize<List<RespuestaDadosModel>>(respuestasJson);
                var config = JsonSerializer.Deserialize<ConfDadosModel>(configJson);

                int correctas = respuestas.Count(r => r.Es_Correcta);
                
                int sinResponder = respuestas.Count(r => string.IsNullOrEmpty(r.Respuesta_Usuario));
                int incorrectas = ejercicios.Count - correctas - sinResponder;

                double porcentajeAcierto = ejercicios.Count > 0 ? (correctas * 100.0) / ejercicios.Count : 0;
                double tiempoPromedio = respuestas.Where(r => r.Tiempo_Respuesta > 0).DefaultIfEmpty().Average(r => r?.Tiempo_Respuesta ?? 0);
                double tiempoTotal = respuestas.Sum(r => r.Tiempo_Respuesta);

                var modeloResultados = new ResDadosViewModel
                {
                    Ejercicios = ejercicios,
                    Respuestas = respuestas,
                    Configuracion = config,
                    TotalCorrectas = correctas,
                    TotalIncorrectas = incorrectas,
                    TotalSinResponder = sinResponder,
                    PorcentajeAcierto = porcentajeAcierto,
                    TiempoPromedio = tiempoPromedio,
                    TiempoTotal = tiempoTotal,
                    FechaPrueba = DateTime.Now
                };

                HttpContext.Session.Remove("DadosEjercicios");
                HttpContext.Session.Remove("DadosRespuestas");
                //HttpContext.Session.Remove("DadosConf");
                HttpContext.Session.Remove("EjercicioActualDados");
                HttpContext.Session.Remove("DadosTiempoRestante");
                HttpContext.Session.Remove("DadosInicioTiempo");

                PruebasDBModel result = new PruebasDBModel
                {
                    Id_Usuario = userId.Value,
                    Total_Preguntas = ejercicios.Count,
                    Respuestas_Correctas = correctas,
                    Tiempo = TimeSpan.FromSeconds(tiempoTotal),
                    Fecha = DateTime.Now,
                    ExperienciaAdquirida = (int)porcentajeAcierto,
                    Tipo_Prueba = "Matemáticas con Dados tradicional"
                };

                bool InsertarPrueba = _pruebasDBService.GuardarPrueba(result);

                return View(modeloResultados);

            }
            catch (Exception ex)
            {
                return RedirectToAction("FormularioDados");
            }
        }

        [HttpGet]
        public IActionResult RepetirDados()
        {
            try
            {
                var configJson = HttpContext.Session.GetString("DadosConf");

                if (string.IsNullOrEmpty(configJson))
                {
                    return RedirectToAction("FormularioDados");
                }

                var config = JsonSerializer.Deserialize<ConfDadosModel>(configJson);

                List<EjercicioDadosModel> ejercicios = GenerarEjerciciosDados(config);

                List<RespuestaDadosModel> respuestas = ejercicios.Select(e => new RespuestaDadosModel
                {
                    Id_Ejercicio = e.Id_Ejercicio,
                    Respuesta_Usuario = string.Empty,
                    Es_Correcta = false,
                    Tiempo_Respuesta = 0,
                    DadosUtilizados = 0
                }).ToList();

                HttpContext.Session.SetString("DadosEjercicios", JsonSerializer.Serialize(ejercicios));
                HttpContext.Session.SetString("DadosRespuestas", JsonSerializer.Serialize(respuestas));
                HttpContext.Session.SetInt32("EjercicioActualDados", 1);
                HttpContext.Session.SetInt32("DadosTiempoRestante", config.TiempoTotal * 60);
                HttpContext.Session.SetString("DadosInicioTiempo", DateTime.Now.ToString());
                HttpContext.Session.SetInt32("DadosSaltosUsados", 0);

                ViewBag.TiempoMeditacion = config.TiempoMeditacion;

                if (config.ModoJuego == "SumaFlash")
                {
                    return View("ConcentracionSuma");
                }
                else if (config.ModoJuego == "MultiSuma")
                {
                    return View("ConcentracionMulti");
                }
                else if (config.ModoJuego == "Dictado")
                {
                    return View("ConcentracionDictado");
                }
                else
                {
                    return View("ConcentracionDados");
                }
            }
            catch (Exception)
            {
                return RedirectToAction("FormularioDados");
            }
        }

        private bool ValidarRespuestaMatematica(string respuestaUsuario, int[] dados, int resultadoEsperado, bool usaJerarquia)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(respuestaUsuario)) return false;

                string expresion = respuestaUsuario.Trim().Replace(" ", "");

                if (!EsExpresionValida(expresion, dados)) return false;

                
                double resultadoCalculado = EvaluarExpresion(expresion, usaJerarquia);

                return Math.Abs(resultadoCalculado - resultadoEsperado) < 0.0001;
            }
            catch { return false; }
        }

        private bool EsExpresionValida(string expresion, int[] dados)
        {
            try
            {
                foreach (char c in expresion)
                {
                    if (!char.IsDigit(c) && !"+*-/()^".Contains(c))
                        return false;
                }

                var numerosUsados = ExtraerNumerosDeExpresion(expresion);

                if (numerosUsados.Count < 2)
                    return false;

                var dadosDisponibles = dados.ToList();
                foreach (int num in numerosUsados)
                {
                    if (!dadosDisponibles.Contains(num))
                        return false;
                    dadosDisponibles.Remove(num);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private List<int> ExtraerNumerosDeExpresion(string expresion)
        {
            var numeros = new List<int>();
            string numeroActual = "";

            foreach (char c in expresion + " ")
            {
                if (char.IsDigit(c))
                {
                    numeroActual += c;
                }
                else if (numeroActual != "")
                {
                    if (int.TryParse(numeroActual, out int num))
                    {
                        numeros.Add(num);
                    }
                    numeroActual = "";
                }
            }

            return numeros;
        }

        private double EvaluarExpresion(string expresion, bool usaJerarquia)
        {
            try
            {
                expresion = expresion.Replace("×", "*").Replace("÷", "/");

                if (usaJerarquia)
                {
                    // LÓGICA ESTÁNDAR (MATEMÁTICA)
                    if (expresion.Contains('^')) return EvaluarExpresionConPotencias(expresion);

                    var dataTable = new System.Data.DataTable();
                    return Convert.ToDouble(dataTable.Compute(expresion, ""));
                }
                else
                {
                    // LÓGICA LINEAL (CALCULADORA BÁSICA)
                    return EvaluarLineal(expresion);
                }
            }
            catch { throw new Exception("Error en la expresión"); }
        }

        private double EvaluarLineal(string expresion)
        {
            // Usamos Regex para separar números y operadores manteniendo el orden
            var tokens = System.Text.RegularExpressions.Regex.Matches(expresion, @"(\d+)|([\+\-\*\/\^])");

            if (tokens.Count == 0) return 0;

            double resultado = double.Parse(tokens[0].Value);

            for (int i = 1; i < tokens.Count; i += 2)
            {
                string op = tokens[i].Value;
                double siguienteNum = double.Parse(tokens[i + 1].Value);

                switch (op)
                {
                    case "+": resultado += siguienteNum; break;
                    case "-": resultado -= siguienteNum; break;
                    case "*": resultado *= siguienteNum; break;
                    case "/": resultado /= siguienteNum; break;
                    case "^": resultado = Math.Pow(resultado, siguienteNum); break;
                }
            }
            return resultado;
        }

        private double EvaluarExpresionConPotencias(string expresion)
        {
            try
            {
                // Evaluar manualmente las potencias
                while (expresion.Contains('^'))
                {
                    int index = expresion.IndexOf('^');

                    // Encontrar la base (número antes del ^)
                    string baseStr = ExtraerNumeroAntes(expresion, index - 1);
                    // Encontrar el exponente (número después del ^)
                    string expStr = ExtraerNumeroDespues(expresion, index + 1);

                    if (double.TryParse(baseStr, out double baseNum) && double.TryParse(expStr, out double expNum))
                    {
                        double resultado = Math.Pow(baseNum, expNum);
                        expresion = expresion.Replace($"{baseStr}^{expStr}", resultado.ToString());
                    }
                    else
                    {
                        throw new Exception("Error al evaluar potencia");
                    }
                }

                // Evaluar el resto de la expresión
                var dataTable = new System.Data.DataTable();
                var valor = dataTable.Compute(expresion, "");
                return Convert.ToDouble(valor);
            }
            catch
            {
                throw new Exception("Error al evaluar expresión con potencias");
            }
        }

        private string ExtraerNumeroAntes(string expresion, int index)
        {
            string numero = "";
            for (int i = index; i >= 0; i--)
            {
                if (char.IsDigit(expresion[i]))
                {
                    numero = expresion[i] + numero;
                }
                else
                {
                    break;
                }
            }
            return numero;
        }

        private string ExtraerNumeroDespues(string expresion, int index)
        {
            string numero = "";
            for (int i = index; i < expresion.Length; i++)
            {
                if (char.IsDigit(expresion[i]))
                {
                    numero += expresion[i];
                }
                else
                {
                    break;
                }
            }
            return numero;
        }

        private int ContarDadosUtilizados(string respuesta, int[] dados)
        {
            var numerosUsados = ExtraerNumerosDeExpresion(respuesta);
            return numerosUsados.Count();
        }

        //segundo modo
        [HttpPost]
        public IActionResult ConcentracionSuma(ConfDadosModel config)
        {
            try
            {
                // Forzamos el modo de juego para que el generador sepa qué hacer
                config.ModoJuego = "SumaFlash";

                // 1. Generar los ejercicios (Asegúrate de que GenerarEjerciciosDados use config.ModoJuego)
                List<EjercicioDadosModel> ejercicios = GenerarEjerciciosDados(config);

                // 2. Generar la lista de respuestas iniciales
                List<RespuestaDadosModel> respuestas = ejercicios.Select(e => new RespuestaDadosModel
                {
                    Id_Ejercicio = e.Id_Ejercicio,
                    Respuesta_Usuario = string.Empty,
                    Es_Correcta = false,
                    Tiempo_Respuesta = 0,
                    DadosUtilizados = 0
                }).ToList();

                // 3. Guardar en session de forma independiente
                HttpContext.Session.SetInt32("DadosSaltosUsados", 0);
                HttpContext.Session.SetString("DadosConf", JsonSerializer.Serialize(config));
                HttpContext.Session.SetString("DadosEjercicios", JsonSerializer.Serialize(ejercicios));
                HttpContext.Session.SetString("DadosRespuestas", JsonSerializer.Serialize(respuestas));
                HttpContext.Session.SetInt32("EjercicioActualDados", 1);

                // Convertir minutos a segundos para el contador
                HttpContext.Session.SetInt32("DadosTiempoRestante", config.TiempoTotal * 60);
                HttpContext.Session.SetString("DadosInicioTiempo", DateTime.Now.ToString());

                // 4. Pasar el tiempo de meditación a la vista
                ViewBag.TiempoMeditacion = config.TiempoMeditacion;

                // Retorna la vista ConcentracionSuma.cshtml
                return View();
            }
            catch (Exception ex)
            {
                // Si algo falla, regresamos al formulario de configuración
                return RedirectToAction("FormularioDados");
            }
        }

        [HttpGet]
        public IActionResult EjercicioSuma()
        {
            try
            {
                // 1. Recuperar datos esenciales de la sesión
                var configJson = HttpContext.Session.GetString("DadosConf");
                var ejerciciosJson = HttpContext.Session.GetString("DadosEjercicios");

                if (string.IsNullOrEmpty(configJson) || string.IsNullOrEmpty(ejerciciosJson))
                {
                    return RedirectToAction("FormularioDados");
                }

                // 2. Deserializar
                List<EjercicioDadosModel> ejercicios = JsonSerializer.Deserialize<List<EjercicioDadosModel>>(ejerciciosJson);
                ConfDadosModel config = JsonSerializer.Deserialize<ConfDadosModel>(configJson);

                // 3. Obtener el índice actual (por defecto 1)
                int ejercicioActual = HttpContext.Session.GetInt32("EjercicioActualDados") ?? 1;

                // 4. ¿Ya terminó todos los ejercicios?
                if (ejercicioActual > ejercicios.Count)
                {
                    return RedirectToAction("ResultadosSuma"); // Redirige a la vista de resultados de este modo
                }

                // 5. Validar tiempo restante
                int tiempoRestante = HttpContext.Session.GetInt32("DadosTiempoRestante") ?? 0;

                // Permitir 0 si es modo infinito
                if (config.TiempoTotal > 0 && tiempoRestante <= 0)
                {
                    return RedirectToAction("ResultadosSuma");
                }

                // 6. Obtener el ejercicio específico
                EjercicioDadosModel ejercicio = ejercicios.FirstOrDefault(e => e.Id_Ejercicio == ejercicioActual);

                if (ejercicio == null)
                {
                    return RedirectToAction("FormularioDados");
                }

                // 7. Preparar ViewBag para la UI (Barra de progreso, timer, etc.)
                ViewBag.EjercicioActual = ejercicioActual;
                ViewBag.TotalEjercicios = ejercicios.Count;
                ViewBag.TiempoRestante = tiempoRestante;
                ViewBag.ModoJuego = config.ModoJuego;
                ViewBag.SaltosUsados = HttpContext.Session.GetInt32("DadosSaltosUsados") ?? 0;

                // Retornamos la vista específica para Suma
                return View(ejercicio);
            }
            catch (Exception)
            {
                return RedirectToAction("FormularioDados");
            }
        }

        [HttpPost]
        public IActionResult EjercicioSuma(int respuestaUsuario, double tiempoRespuesta, int tiempoRestanteActual, bool esSalto = false)
        {
            try
            {
                // 1. Recuperamos los datos
                var respuestasJson = HttpContext.Session.GetString("DadosRespuestas");
                var ejerciciosJson = HttpContext.Session.GetString("DadosEjercicios");
                var ejercicioActual = HttpContext.Session.GetInt32("EjercicioActualDados") ?? 1;
                var saltosUsados = HttpContext.Session.GetInt32("DadosSaltosUsados") ?? 0;
                var configJson = HttpContext.Session.GetString("DadosConf");

                if (string.IsNullOrEmpty(respuestasJson) || string.IsNullOrEmpty(ejerciciosJson) || string.IsNullOrEmpty(configJson))
                    return RedirectToAction("FormularioDados");

                var respuestas = JsonSerializer.Deserialize<List<RespuestaDadosModel>>(respuestasJson);
                var ejercicios = JsonSerializer.Deserialize<List<EjercicioDadosModel>>(ejerciciosJson);
                ConfDadosModel config = JsonSerializer.Deserialize<ConfDadosModel>(configJson);
                var ejercicio = ejercicios.FirstOrDefault(e => e.Id_Ejercicio == ejercicioActual);
                var respuesta = respuestas.FirstOrDefault(r => r.Id_Ejercicio == ejercicioActual);

                if (esSalto && saltosUsados < 2)
                {
                    // Lógica de Salto
                    respuesta.Respuesta_Usuario = string.Empty; // Queda como "No respondió"
                    respuesta.Es_Correcta = false;
                    respuesta.Tiempo_Respuesta = tiempoRespuesta;

                    saltosUsados++;
                    HttpContext.Session.SetInt32("DadosSaltosUsados", saltosUsados);
                }
                else
                {
                    // Lógica de respuesta normal
                    respuesta.Respuesta_Usuario = respuestaUsuario.ToString();
                    respuesta.Tiempo_Respuesta = tiempoRespuesta;
                    respuesta.Es_Correcta = (respuestaUsuario == ejercicio.Dado_Resultado);
                }

                HttpContext.Session.SetInt32("DadosTiempoRestante", tiempoRestanteActual);

                // Guardamos respuestas y avanzamos
                HttpContext.Session.SetString("DadosRespuestas", JsonSerializer.Serialize(respuestas));
                HttpContext.Session.SetInt32("EjercicioActualDados", ejercicioActual + 1);

                // Aquí ya recibes tiempoRestanteActual desde el JS de la vista
                // Solo validamos la salida
                if (ejercicioActual + 1 > ejercicios.Count || (config.TiempoTotal > 0 && tiempoRestanteActual <= 0))
                {
                    return RedirectToAction("ResultadosSuma");
                }

                return RedirectToAction("EjercicioSuma");
            }
            catch
            {
                return RedirectToAction("FormularioDados");
            }
        }

        [HttpGet]
        public IActionResult ResultadosSuma()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("Id_Usuario");
                var configJson = HttpContext.Session.GetString("DadosConf");
                var ejerciciosJson = HttpContext.Session.GetString("DadosEjercicios");
                var respuestasJson = HttpContext.Session.GetString("DadosRespuestas");

                if (string.IsNullOrEmpty(ejerciciosJson) || string.IsNullOrEmpty(respuestasJson))
                    return RedirectToAction("FormularioDados");

                var ejercicios = JsonSerializer.Deserialize<List<EjercicioDadosModel>>(ejerciciosJson);
                var respuestas = JsonSerializer.Deserialize<List<RespuestaDadosModel>>(respuestasJson);
                var config = JsonSerializer.Deserialize<ConfDadosModel>(configJson);

                // --- LÓGICA DE CONTEO ---
                int totalCorrectas = respuestas.Count(r => r.Es_Correcta);
                // Se considera "No respondió" si el string está vacío o es null
                int noRespondidas = respuestas.Count(r => string.IsNullOrEmpty(r.Respuesta_Usuario));
                int incorrectas = ejercicios.Count - totalCorrectas - noRespondidas;

                double tiempoTotal = respuestas.Sum(r => r.Tiempo_Respuesta);
                double promedio = respuestas.Where(r => r.Tiempo_Respuesta > 0).DefaultIfEmpty().Average(r => r?.Tiempo_Respuesta ?? 0);
                double porcentajeCalculado = ejercicios.Count > 0 ? (totalCorrectas * 100.0) / ejercicios.Count : 0;
                // Preparar el ViewModel para la vista (puedes usar el mismo de Dados o uno nuevo)
                var viewModel = new ResDadosViewModel
                {
                    Ejercicios = ejercicios,
                    Respuestas = respuestas,
                    TotalCorrectas = totalCorrectas,
                    TotalIncorrectas = incorrectas,
                    TotalSinResponder = noRespondidas,
                    TiempoTotal = tiempoTotal,
                    TiempoPromedio = promedio,
                    Configuracion = config,
                    PorcentajeAcierto = porcentajeCalculado
                };

                // Guardar en DB antes de limpiar sesión
                PruebasDBModel record = new PruebasDBModel
                {
                    Id_Usuario = userId ?? 0,
                    Total_Preguntas = ejercicios.Count,
                    Respuestas_Correctas = totalCorrectas,
                    Tiempo = TimeSpan.FromSeconds(tiempoTotal),
                    Fecha = DateTime.Now,
                    Tipo_Prueba = "Matemáticas con Dados suma",
                    ExperienciaAdquirida = totalCorrectas * 5 // Lógica de XP
                };
                _pruebasDBService.GuardarPrueba(record);

                return View(viewModel);
            }
            catch { return RedirectToAction("FormularioDados"); }
        }

        //tercer modo
        [HttpPost]
        public IActionResult ConcentracionMulti(ConfDadosModel config)
        {
            try
            {
                // 1. Forzamos la identidad del modo
                config.ModoJuego = "MultiSuma";

                // 2. Generamos la lista de ejercicios bajo las nuevas reglas
                List<EjercicioDadosModel> ejercicios = GenerarEjerciciosDados(config);

                int totalDadosMulti = (config.NumeroDados > 0 ? config.NumeroDados : 5) + 1;

                // 3. Inicializamos las respuestas vacías
                List<RespuestaDadosModel> respuestas = ejercicios.Select(e => new RespuestaDadosModel
                {
                    Id_Ejercicio = e.Id_Ejercicio,
                    Respuesta_Usuario = string.Empty,
                    Es_Correcta = false,
                    Tiempo_Respuesta = 0,
                    DadosUtilizados = totalDadosMulti
                }).ToList();

                // 4. Persistencia en Sesión
                HttpContext.Session.SetString("DadosConf", JsonSerializer.Serialize(config));
                HttpContext.Session.SetString("DadosEjercicios", JsonSerializer.Serialize(ejercicios));
                HttpContext.Session.SetString("DadosRespuestas", JsonSerializer.Serialize(respuestas));
                HttpContext.Session.SetInt32("EjercicioActualDados", 1);
                HttpContext.Session.SetInt32("DadosTiempoRestante", config.TiempoTotal * 60);
                HttpContext.Session.SetInt32("DadosSaltosUsados", 0); // Reset de comodines

                ViewBag.TiempoMeditacion = config.TiempoMeditacion;

                return View();
            }
            catch (Exception)
            {
                return RedirectToAction("FormularioDados");
            }
        }

        [HttpGet]
        public IActionResult EjercicioMulti()
        {
            try
            {
                // 1. Recuperar datos de sesión
                var configJson = HttpContext.Session.GetString("DadosConf");
                var ejerciciosJson = HttpContext.Session.GetString("DadosEjercicios");

                if (string.IsNullOrEmpty(configJson) || string.IsNullOrEmpty(ejerciciosJson))
                    return RedirectToAction("FormularioDados");

                var ejercicios = JsonSerializer.Deserialize<List<EjercicioDadosModel>>(ejerciciosJson);
                var config = JsonSerializer.Deserialize<ConfDadosModel>(configJson);

                int ejercicioActual = HttpContext.Session.GetInt32("EjercicioActualDados") ?? 1;
                int tiempoRestante = HttpContext.Session.GetInt32("DadosTiempoRestante") ?? 0;
                int saltosUsados = HttpContext.Session.GetInt32("DadosSaltosUsados") ?? 0;

                // 2. Validar fin de juego o tiempo agotado
                if (ejercicioActual > ejercicios.Count || (tiempoRestante <= 0 && config.TiempoTotal > 0))
                    return RedirectToAction("ResultadosMulti");

                // 3. Obtener el ejercicio específico
                var ejercicio = ejercicios.FirstOrDefault(e => e.Id_Ejercicio == ejercicioActual);
                if (ejercicio == null) return RedirectToAction("FormularioDados");

                // 4. Pasar info a la UI
                ViewBag.EjercicioActual = ejercicioActual;
                ViewBag.TotalEjercicios = ejercicios.Count;
                ViewBag.TiempoRestante = tiempoRestante;
                ViewBag.SaltosUsados = saltosUsados;
                ViewBag.ModoJuego = config.ModoJuego;

                return View(ejercicio);
            }
            catch { return RedirectToAction("FormularioDados"); }
        }

        [HttpPost]
        public IActionResult EjercicioMulti(int? respuestaUsuario, double tiempoRespuesta, int tiempoRestanteActual, bool esSalto = false)
        {
            try
            {
                var respuestasJson = HttpContext.Session.GetString("DadosRespuestas");
                var ejerciciosJson = HttpContext.Session.GetString("DadosEjercicios");
                var ejercicioActual = HttpContext.Session.GetInt32("EjercicioActualDados") ?? 1;
                var saltosUsados = HttpContext.Session.GetInt32("DadosSaltosUsados") ?? 0;
                var configJson = HttpContext.Session.GetString("DadosConf");

                if (string.IsNullOrEmpty(respuestasJson) || string.IsNullOrEmpty(ejerciciosJson) || string.IsNullOrEmpty(configJson))
                    return RedirectToAction("FormularioDados");

                var respuestas = JsonSerializer.Deserialize<List<RespuestaDadosModel>>(respuestasJson);
                var ejercicios = JsonSerializer.Deserialize<List<EjercicioDadosModel>>(ejerciciosJson);
                var config = JsonSerializer.Deserialize<ConfDadosModel>(configJson);
                var ejercicio = ejercicios.FirstOrDefault(e => e.Id_Ejercicio == ejercicioActual);
                var respuesta = respuestas.FirstOrDefault(r => r.Id_Ejercicio == ejercicioActual);

                if (ejercicio != null && respuesta != null)
                {
                    if (esSalto && saltosUsados < 2)
                    {
                        // Lógica de SALTO
                        respuesta.Respuesta_Usuario = string.Empty;
                        respuesta.Es_Correcta = false;
                        saltosUsados++;
                        HttpContext.Session.SetInt32("DadosSaltosUsados", saltosUsados);
                    }
                    else
                    {
                        // Lógica de RESPUESTA
                        respuesta.Respuesta_Usuario = respuestaUsuario?.ToString() ?? "0";
                        respuesta.Es_Correcta = (respuestaUsuario == ejercicio.Dado_Resultado);
                    }

                    respuesta.Tiempo_Respuesta = tiempoRespuesta;
                    respuesta.DadosUtilizados = (config.NumeroDados > 0 ? config.NumeroDados : 5) + 1;
                }

                // Sincronizar tiempo y avanzar
                HttpContext.Session.SetInt32("DadosTiempoRestante", tiempoRestanteActual);
                HttpContext.Session.SetString("DadosRespuestas", JsonSerializer.Serialize(respuestas));
                HttpContext.Session.SetInt32("EjercicioActualDados", ejercicioActual + 1);

                // Redirección final
                if (ejercicioActual + 1 > ejercicios.Count || (config.TiempoTotal > 0 && tiempoRestanteActual <= 0))
                {
                    return RedirectToAction("ResultadosMulti");
                }

                return RedirectToAction("EjercicioMulti");
            }
            catch { return RedirectToAction("FormularioDados"); }
        }

        [HttpGet]
        public IActionResult ResultadosMulti()
        {
            try
            {
                // 1. Recuperamos la info de sesión como en tus otros métodos
                var userId = HttpContext.Session.GetInt32("Id_Usuario");
                var configJson = HttpContext.Session.GetString("DadosConf");
                var ejerciciosJson = HttpContext.Session.GetString("DadosEjercicios");
                var respuestasJson = HttpContext.Session.GetString("DadosRespuestas");

                // 2. Validación de seguridad
                if (string.IsNullOrEmpty(ejerciciosJson) || string.IsNullOrEmpty(respuestasJson))
                    return RedirectToAction("FormularioDados");

                // 3. Deserialización con System.Text.Json
                var ejercicios = JsonSerializer.Deserialize<List<EjercicioDadosModel>>(ejerciciosJson);
                var respuestas = JsonSerializer.Deserialize<List<RespuestaDadosModel>>(respuestasJson);
                var config = JsonSerializer.Deserialize<ConfDadosModel>(configJson);

                // --- LÓGICA DE CONTEO ---
                int totalCorrectas = respuestas.Count(r => r.Es_Correcta);
                // Se considera "No respondió" si el string está vacío o es null
                int noRespondidas = respuestas.Count(r => string.IsNullOrEmpty(r.Respuesta_Usuario));
                int incorrectas = ejercicios.Count - totalCorrectas - noRespondidas;

                double tiempoTotal = respuestas.Sum(r => r.Tiempo_Respuesta);
                double promedio = respuestas.Where(r => r.Tiempo_Respuesta > 0).DefaultIfEmpty().Average(r => r?.Tiempo_Respuesta ?? 0);
                double porcentajeCalculado = ejercicios.Count > 0 ? (totalCorrectas * 100.0) / ejercicios.Count : 0;

                // 4. Preparar el ViewModel (usando tu modelo ResDadosViewModel)
                var viewModel = new ResDadosViewModel
                {
                    Ejercicios = ejercicios,
                    Respuestas = respuestas,
                    TotalCorrectas = totalCorrectas,
                    TotalIncorrectas = incorrectas,
                    TotalSinResponder = noRespondidas,
                    TiempoTotal = tiempoTotal,
                    TiempoPromedio = promedio,
                    Configuracion = config,
                    PorcentajeAcierto = porcentajeCalculado
                };

                // 5. Registro en DB (Cambiamos el Tipo_Prueba a Multi)
                PruebasDBModel record = new PruebasDBModel
                {
                    Id_Usuario = userId ?? 0,
                    Total_Preguntas = ejercicios.Count,
                    Respuestas_Correctas = totalCorrectas,
                    Tiempo = TimeSpan.FromSeconds(tiempoTotal),
                    Fecha = DateTime.Now,
                    Tipo_Prueba = "Matemáticas con Dados multi",
                    ExperienciaAdquirida = totalCorrectas * 5 // Mantengo tu lógica de XP
                };
                _pruebasDBService.GuardarPrueba(record);

                return View(viewModel);
            }
            catch
            {
                return RedirectToAction("FormularioDados");
            }
        }


        // ==========================================
        // MODO DICTADO (INSTRUCCIÓN)
        // ==========================================

        [HttpPost]
        public IActionResult ConcentracionDictado(ConfDadosModel config)
        {
            try
            {
                config.ModoJuego = "Dictado";

                List<EjercicioDadosModel> ejercicios = GenerarEjerciciosDados(config);

                List<RespuestaDadosModel> respuestas = ejercicios.Select(e => new RespuestaDadosModel
                {
                    Id_Ejercicio = e.Id_Ejercicio,
                    Respuesta_Usuario = string.Empty,
                    Es_Correcta = false,
                    Tiempo_Respuesta = 0,
                    DadosUtilizados = 0
                }).ToList();

                // Guardar en sesión
                HttpContext.Session.SetString("DadosConf", JsonSerializer.Serialize(config));
                HttpContext.Session.SetString("DadosEjercicios", JsonSerializer.Serialize(ejercicios));
                HttpContext.Session.SetString("DadosRespuestas", JsonSerializer.Serialize(respuestas));
                HttpContext.Session.SetInt32("EjercicioActualDados", 1);

                
                HttpContext.Session.SetInt32("DadosTiempoRestante", config.TiempoTotal * 60);

                ViewBag.TiempoMeditacion = config.TiempoMeditacion;
                return View();
            }
            catch (Exception)
            {
                return RedirectToAction("FormularioDados");
            }
        }

        [HttpGet]
        public IActionResult EjercicioDictado()
        {
            try
            {
                var configJson = HttpContext.Session.GetString("DadosConf");
                var ejerciciosJson = HttpContext.Session.GetString("DadosEjercicios");

                if (string.IsNullOrEmpty(configJson) || string.IsNullOrEmpty(ejerciciosJson))
                {
                    return RedirectToAction("FormularioDados");
                }

                List<EjercicioDadosModel> ejercicios = JsonSerializer.Deserialize<List<EjercicioDadosModel>>(ejerciciosJson);
                ConfDadosModel config = JsonSerializer.Deserialize<ConfDadosModel>(configJson);

                int ejercicioActual = HttpContext.Session.GetInt32("EjercicioActualDados") ?? 1;

                if (ejercicioActual > ejercicios.Count)
                {
                    return RedirectToAction("ResultadosDictado");
                }

                EjercicioDadosModel ejercicio = ejercicios.FirstOrDefault(e => e.Id_Ejercicio == ejercicioActual);

                if (ejercicio == null)
                {
                    return RedirectToAction("FormularioDados");
                }

                ViewBag.EjercicioActual = ejercicioActual;
                ViewBag.TotalEjercicios = ejercicios.Count;

                return View(ejercicio);
            }
            catch (Exception)
            {
                return RedirectToAction("FormularioDados");
            }
        }

        [HttpPost]
        public IActionResult EjercicioDictado(int dadosUsados, bool esCorrecta, double tiempoRespuesta, bool esSalto = false)
        {
            try
            {
                var respuestasJson = HttpContext.Session.GetString("DadosRespuestas");
                var ejerciciosJson = HttpContext.Session.GetString("DadosEjercicios");
                var ejercicioActual = HttpContext.Session.GetInt32("EjercicioActualDados") ?? 1;

                if (string.IsNullOrEmpty(respuestasJson) || string.IsNullOrEmpty(ejerciciosJson))
                    return RedirectToAction("FormularioDados");

                var respuestas = JsonSerializer.Deserialize<List<RespuestaDadosModel>>(respuestasJson);
                var ejercicios = JsonSerializer.Deserialize<List<EjercicioDadosModel>>(ejerciciosJson);
                var respuesta = respuestas.FirstOrDefault(r => r.Id_Ejercicio == ejercicioActual);

                if (respuesta != null)
                {
                    if (esSalto)
                    {
                        // Si se presionó el botón "?", lo guardamos como cadena vacía
                        respuesta.DadosUtilizados = 0;
                        respuesta.Es_Correcta = false;
                        respuesta.Tiempo_Respuesta = tiempoRespuesta;
                        respuesta.Respuesta_Usuario = string.Empty;
                    }
                    else
                    {
                        // Flujo normal (Correcto o Incorrecto)
                        respuesta.DadosUtilizados = dadosUsados;
                        respuesta.Es_Correcta = esCorrecta;
                        respuesta.Tiempo_Respuesta = tiempoRespuesta;
                        respuesta.Respuesta_Usuario = esCorrecta ? "Correcto" : "Incorrecto";
                    }
                }

                HttpContext.Session.SetString("DadosRespuestas", JsonSerializer.Serialize(respuestas));
                HttpContext.Session.SetInt32("EjercicioActualDados", ejercicioActual + 1);

                if (ejercicioActual + 1 > ejercicios.Count)
                {
                    return RedirectToAction("ResultadosDictado");
                }

                return RedirectToAction("EjercicioDictado");
            }
            catch
            {
                return RedirectToAction("FormularioDados");
            }
        }

        [HttpGet]
        public IActionResult ResultadosDictado()
        {
            try
            {
                var configJson = HttpContext.Session.GetString("DadosConf");
                var ejerciciosJson = HttpContext.Session.GetString("DadosEjercicios");
                var respuestasJson = HttpContext.Session.GetString("DadosRespuestas");

                if (string.IsNullOrEmpty(ejerciciosJson) || string.IsNullOrEmpty(respuestasJson))
                    return RedirectToAction("FormularioDados");

                var ejercicios = JsonSerializer.Deserialize<List<EjercicioDadosModel>>(ejerciciosJson);
                var respuestas = JsonSerializer.Deserialize<List<RespuestaDadosModel>>(respuestasJson);
                var config = JsonSerializer.Deserialize<ConfDadosModel>(configJson);

                // Calculamos las estadísticas separando las No Respondidas
                int totalCorrectas = respuestas.Count(r => r.Es_Correcta);
                int sinResponder = respuestas.Count(r => string.IsNullOrEmpty(r.Respuesta_Usuario));
                int totalIncorrectas = ejercicios.Count - totalCorrectas - sinResponder;

                double tiempoTotal = respuestas.Sum(r => r.Tiempo_Respuesta);
                double promedio = respuestas.Where(r => r.Tiempo_Respuesta > 0).DefaultIfEmpty().Average(r => r?.Tiempo_Respuesta ?? 0);

                var viewModel = new ResDadosViewModel
                {
                    Ejercicios = ejercicios,
                    Respuestas = respuestas,
                    TotalCorrectas = totalCorrectas,
                    TotalIncorrectas = totalIncorrectas,
                    TotalSinResponder = sinResponder, // Se lo pasamos a tu ViewModel
                    TiempoTotal = tiempoTotal,
                    TiempoPromedio = promedio,
                    Configuracion = config
                };

                HttpContext.Session.Remove("DadosEjercicios");
                HttpContext.Session.Remove("DadosRespuestas");
                HttpContext.Session.Remove("EjercicioActualDados");
                HttpContext.Session.Remove("DadosTiempoRestante");
                HttpContext.Session.Remove("DadosInicioTiempo");

                return View(viewModel);
            }
            catch
            {
                return RedirectToAction("FormularioDados");
            }
        }

    }
}