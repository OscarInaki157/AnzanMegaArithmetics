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
                    CantidadEjercicios = 10,
                    TiempoTotal = 5,
                    TiempoMeditacion = 3,
                    TipoPrueba = "Práctica"
                };

                HttpContext.Session.SetString("DadosConf", JsonSerializer.Serialize(config));
            }
            else
            {
                config = JsonSerializer.Deserialize<ConfDadosModel>(configJson)!;
                config.TipoPrueba = "Práctica";
            }

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

            for (int i = 0; i < config.CantidadEjercicios; i++)
            {
                int resultado = _random.Next(1, 13);

                int[] dados = GenerarDadosConSolucion(resultado);

                ejercicios.Add(new EjercicioDadosModel
                {
                    Id_Ejercicio = i + 1,
                    Dados = dados,
                    Dado_Resultado = resultado,
                    SolucionesPosibles = new List<string>()
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

                if (tiempoRestante <= 0)
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

                respuesta.Respuesta_Usuario = respuestaUsuario;
                respuesta.Tiempo_Respuesta = tiempoRespuesta;

                respuesta.Es_Correcta = ValidarRespuestaMatematica(respuestaUsuario, ejercicio.Dados, ejercicio.Dado_Resultado);
                respuesta.DadosUtilizados = ContarDadosUtilizados(respuestaUsuario, ejercicio.Dados);

                HttpContext.Session.SetString("DadosRespuestas", JsonSerializer.Serialize(respuestas));

                if (tiempoRestante > 0)
                {
                    tiempoRestante = Math.Max(0, tiempoRestante - (int)Math.Ceiling(tiempoRespuesta));
                }

                HttpContext.Session.SetInt32("DadosTiempoRestante", tiempoRestante);
                HttpContext.Session.SetInt32("EjercicioActualDados", ejercicioActual + 1);

                if (ejercicioActual + 1 > ejercicios.Count || tiempoRestante <= 0)
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
                int incorrectas = respuestas.Count(r => !r.Es_Correcta);
                int sinResponder = ejercicios.Count - respuestas.Count(r => string.IsNullOrEmpty(r.Respuesta_Usuario));

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
                    Tipo_Prueba = "Matemáticas con Dados"
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
                return View("ConcentracionDados");
            }
            catch (Exception ex)
            {
                return RedirectToAction("FormularioDados");
            }
        }

        private bool ValidarRespuestaMatematica(string respuestaUsuario, int[] dados, int resultadoEsperado)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(respuestaUsuario))
                    return false;

                string expresion = respuestaUsuario.Trim().Replace(" ", "");

                if (!EsExpresionValida(expresion, dados))
                    return false;

                double resultadoCalculado = EvaluarExpresion(expresion);

                return Math.Abs(resultadoCalculado - resultadoEsperado) < 0.0001;
            }
            catch
            {
                return false;
            }
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

        private double EvaluarExpresion(string expresion)
        {
            try
            {
                expresion = expresion.Replace("×", "*").Replace("÷", "/");

                if (expresion.Contains('^'))
                {
                    return EvaluarExpresionConPotencias(expresion);
                }
                else
                {
                    var dataTable = new System.Data.DataTable();
                    var valor = dataTable.Compute(expresion, "");
                    return Convert.ToDouble(valor);
                }
            }
            catch
            {
                throw new Exception("Expresión matemática inválida");
            }
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
            return numerosUsados.Distinct().Count();
        }
    }
}