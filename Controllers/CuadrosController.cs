using AnzanMegaArithmetics.Models;
using AnzanMegaArithmetics.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AnzanMegaArithmetics.Controllers
{
    [Authorize]
    public class CuadrosController : Controller
    {
        private readonly IPruebasDBService _pruebasDBService;
        public CuadrosController(IPruebasDBService pruebasDBService)
        {
            _pruebasDBService = pruebasDBService;
        }

        [HttpGet]
        public IActionResult CuadrosForm(int? CantidadRejillas, string DimensionRejilla, string TipoIluminacion, string TipoOperacion, string DigitosSuma, string DigitosResta, int? TiempoMeditacion)
        {
            var userId = HttpContext.Session.GetInt32("Id_Usuario");

            if (userId == null || userId == 0)
            {
                return RedirectToAction("Inicio", "Inicio");
            }

            ConfCuadrosModel config;

            if (!CantidadRejillas.HasValue || string.IsNullOrEmpty(DimensionRejilla) 
                || string.IsNullOrEmpty(TipoIluminacion) || string.IsNullOrEmpty(DigitosSuma) || string.IsNullOrEmpty(DigitosResta))
            {
                config = new ConfCuadrosModel
                {
                    CantidadRejillas = 1,
                    DimensionRejilla = "10X5",
                    TipoIluminacion = "ninguna",
                    TipoOperacion = "suma",
                    DigitosSuma = "1,2,3,4,5,6,7,8,9",
                    DigitosResta = "1,2,3,4,5,6,7,8,9",
                    TiempoMeditacion = 3,
                    TipoPrueba = "Práctica"
                };
            }
            else 
            {
                config = new ConfCuadrosModel
                {
                    CantidadRejillas = CantidadRejillas.Value,
                    DimensionRejilla = DimensionRejilla,
                    TipoIluminacion = TipoIluminacion,
                    TipoOperacion = TipoOperacion,
                    DigitosSuma = DigitosSuma,
                    DigitosResta = DigitosResta,
                    TiempoMeditacion = TiempoMeditacion.Value,
                    TipoPrueba = "Práctica"
                };
            }
            
            return View(config);
        }

        [HttpGet]
        public IActionResult LimpiarYDashboard() 
        {
            HttpContext.Session.Remove("actuales");
            HttpContext.Session.Remove("respuestas");
            HttpContext.Session.Remove("configuracion");

            return RedirectToAction("Desafios", "Dashboard");
        }

        [HttpGet]
        public IActionResult ConcentracionPractica()
        {
            try
            {
                var configuracionJson = HttpContext.Session.GetString("ConfiguracionPractica");

                if (string.IsNullOrEmpty(configuracionJson))
                {
                    return RedirectToAction("CuadrosForm");
                }

                var config = JsonSerializer.Deserialize<ConfCuadrosModel>(configuracionJson);

                if (config == null)
                {
                    return RedirectToAction("CuadrosForm");
                }

                ViewBag.TiempoMeditacion = config.TiempoMeditacion;

                List<EjercicioCuadrosModel> sesionejercicios = GenerarRejillas(config);

                SesionCuadrosModel sesion = new SesionCuadrosModel
                {
                    ejerciciosCuadros = sesionejercicios,
                    Realizados = 0
                };

                List<RCuadrosModel> respuestas = new();

                HttpContext.Session.SetString("ConfiguracionPractica", JsonSerializer.Serialize(config));
                HttpContext.Session.SetString("Ejercicios", JsonSerializer.Serialize(sesion));
                HttpContext.Session.SetString("Respuestas", JsonSerializer.Serialize(respuestas));

                return View();
            }
            catch (Exception ex)
            {
                return RedirectToAction("CuadrosForm");
            }
        }

        [HttpPost]
        public IActionResult CuadrosCompetencia() 
        {
            ConfCuadrosModel config;
            config = new ConfCuadrosModel
            {
                CantidadRejillas = 3,
                DimensionRejilla = "10X10",
                TipoIluminacion = "ninguna",
                TipoOperacion = "suma",
                DigitosSuma = "1,2,3,4,5,6,7,8,9",
                DigitosResta = "1,2,3,4,5,6,7,8,9",
                TiempoMeditacion = 3,
                TipoPrueba = "Competencia"
            };

            HttpContext.Session.SetString("ConfiguracionPractica", JsonSerializer.Serialize(config));

            return RedirectToAction("ConcentracionPractica");
        }

        [HttpPost]
        public IActionResult ConcentracionPractica(ConfCuadrosModel config)
        {
            ViewBag.TiempoMeditacion = config.TiempoMeditacion;

            List<EjercicioCuadrosModel> sesionejercicios = GenerarRejillas(config);

            SesionCuadrosModel sesion = new SesionCuadrosModel
            {
                ejerciciosCuadros = sesionejercicios,
                Realizados = 0
            };

            List<RCuadrosModel> respuestas = new();

            HttpContext.Session.SetString("ConfiguracionPractica", JsonSerializer.Serialize(config));
            HttpContext.Session.SetString("Ejercicios", JsonSerializer.Serialize(sesion));
            HttpContext.Session.SetString("Respuestas", JsonSerializer.Serialize(respuestas));
            
            return View();
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

            for (int i=0; i < config.CantidadRejillas; i++) 
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

        [HttpGet]
        public IActionResult EjercicioPractica() 
        {
            try
            {
                var configuracionJson = HttpContext.Session.GetString("ConfiguracionPractica");
                var ejerciciosJson = HttpContext.Session.GetString("Ejercicios");


                if (string.IsNullOrEmpty(ejerciciosJson) || string.IsNullOrEmpty(configuracionJson)) 
                {
                    return RedirectToAction("CuadrosForm");
                }

                SesionCuadrosModel actuales = JsonSerializer.Deserialize<SesionCuadrosModel>(ejerciciosJson);
    
                ConfCuadrosModel configuracion = JsonSerializer.Deserialize<ConfCuadrosModel>(configuracionJson);

                if (actuales.Realizados >= configuracion.CantidadRejillas) 
                {
                    //redirigir a resultados
                    return RedirectToAction("ResultadosCuadrosPractica");
                }

                int indice = actuales.Realizados;
                EjercicioCuadrosModel nuevo = actuales.ejerciciosCuadros[indice];

                ViewBag.Configuracion = configuracion;

                return View(nuevo);

            }
            catch (Exception ex) 
            {
                return RedirectToAction("CuadrosForm");
            }
        }

        [HttpPost]
        public IActionResult EjercicioPractica([FromForm] int RespuestaUsuario, [FromForm] string TiempoRespuesta)
        {
            try
            {
                var ejerciciosJson = HttpContext.Session.GetString("Ejercicios");
                var resultadosJson = HttpContext.Session.GetString("Respuestas");
                var configuracionJson = HttpContext.Session.GetString("ConfiguracionPractica");

                if (string.IsNullOrEmpty(ejerciciosJson) || string.IsNullOrEmpty(resultadosJson) || string.IsNullOrEmpty(configuracionJson))
                {
                    return RedirectToAction("CuadrosForm");
                }

                SesionCuadrosModel actuales = JsonSerializer.Deserialize<SesionCuadrosModel>(ejerciciosJson);
                List<RCuadrosModel> respuestas = JsonSerializer.Deserialize<List<RCuadrosModel>>(resultadosJson);
                ConfCuadrosModel configuracion = JsonSerializer.Deserialize<ConfCuadrosModel>(configuracionJson);

                int index = actuales.Realizados;

                // Validar que aún haya ejercicios por responder
                if (index >= actuales.ejerciciosCuadros.Count)
                {
                    return RedirectToAction("ResultadosCuadrosPractica");
                }

                EjercicioCuadrosModel evaluar = actuales.ejerciciosCuadros[index];

                RCuadrosModel nuevaRespuesta = new RCuadrosModel
                {
                    RespuestaUsuario = RespuestaUsuario,
                    RespuestaCorrecta = evaluar.RCorrecta,
                    Respondido = (RespuestaUsuario != -1)
                };

                // Convertir el tiempo a double
                if (double.TryParse(TiempoRespuesta, out double tiempo))
                {
                    nuevaRespuesta.TiempoRespuesta = tiempo;
                }

                respuestas.Add(nuevaRespuesta);
                actuales.Realizados++;

                
                HttpContext.Session.SetString("Ejercicios", JsonSerializer.Serialize(actuales));
                HttpContext.Session.SetString("Respuestas", JsonSerializer.Serialize(respuestas));


                if (actuales.Realizados >= configuracion.CantidadRejillas)
                {
                    return RedirectToAction("ResultadosCuadrosPractica");
                }

                // Redirigir al siguiente ejercicio
                return RedirectToAction("EjercicioPractica");
            }
            catch (Exception ex)
            {
                return RedirectToAction("CuadrosForm");
            }
        }

        [HttpGet]
        public IActionResult ResultadosCuadrosPractica() 
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("Id_Usuario");

                if (userId == null || userId == 0)
                {
                    return RedirectToAction("Inicio", "Inicio");
                }

                var ejerciciosJson = HttpContext.Session.GetString("Ejercicios");
                var resultadosJson = HttpContext.Session.GetString("Respuestas");
                var configuracionJson = HttpContext.Session.GetString("ConfiguracionPractica");

                if (string.IsNullOrEmpty(ejerciciosJson) || string.IsNullOrEmpty(resultadosJson) || string.IsNullOrEmpty(configuracionJson))
                {
                    return RedirectToAction("CuadrosForm");
                }

                SesionCuadrosModel actuales = JsonSerializer.Deserialize<SesionCuadrosModel>(ejerciciosJson);
                List<RCuadrosModel> respuestas = JsonSerializer.Deserialize<List<RCuadrosModel>>(resultadosJson);
                ConfCuadrosModel configuracion = JsonSerializer.Deserialize<ConfCuadrosModel>(configuracionJson);

                double Tiempototal = respuestas.Sum(r => r.TiempoRespuesta);

                ResultadosCuadrosPruebaModel final = new ResultadosCuadrosPruebaModel
                {
                    config = configuracion,
                    ejercicios = actuales.ejerciciosCuadros,
                    respuestas = respuestas
                };

                int total = respuestas.Count;
                int correctas = respuestas.Count(r => r.RespuestaUsuario == r.RespuestaCorrecta);
                int incorrectas = respuestas.Count(r => r.RespuestaUsuario != r.RespuestaCorrecta && r.Respondido);

                int porcentaje = total > 0 ? (correctas * 100) / total : 0;

                PruebasDBModel result = new PruebasDBModel
                {
                    Id_Usuario = userId.Value,
                    Total_Preguntas = total,
                    Tiempo = TimeSpan.FromSeconds(Tiempototal),
                    Respuestas_Correctas = correctas,
                    Fecha = DateTime.Now,
                    ExperienciaAdquirida = porcentaje,
                    Tipo_Prueba = "Cuadros de velocidad " + final.config.TipoPrueba +" : "+ final.config.CantidadRejillas.ToString() + " rejillas " + final.config.DimensionRejilla
                };

                bool InsertarPrueba = _pruebasDBService.GuardarPrueba(result);

                return View(final);

            }
            catch (Exception ex)
            {
                return RedirectToAction("CuadrosForm");
            }
        }

        [HttpPost]
        public IActionResult FinalizarDesdeEjercicio()
        {
            try
            {
                var ejerciciosJson = HttpContext.Session.GetString("Ejercicios");
                var resultadosJson = HttpContext.Session.GetString("Respuestas");
                var configuracionJson = HttpContext.Session.GetString("ConfiguracionPractica");

                if (string.IsNullOrEmpty(ejerciciosJson) || string.IsNullOrEmpty(resultadosJson) || string.IsNullOrEmpty(configuracionJson))
                {
                    return RedirectToAction("CuadrosForm");
                }

                SesionCuadrosModel actuales = JsonSerializer.Deserialize<SesionCuadrosModel>(ejerciciosJson);
                List<RCuadrosModel> respuestas = JsonSerializer.Deserialize<List<RCuadrosModel>>(resultadosJson);
                ConfCuadrosModel configuracion = JsonSerializer.Deserialize<ConfCuadrosModel>(configuracionJson);

                // Marcar los ejercicios restantes como no respondidos
                int ejerciciosRestantes = actuales.ejerciciosCuadros.Count - actuales.Realizados;

                for (int i = 0; i < ejerciciosRestantes; i++)
                {
                    respuestas.Add(new RCuadrosModel
                    {
                        RespuestaUsuario = -1,
                        RespuestaCorrecta = actuales.ejerciciosCuadros[actuales.Realizados + i].RCorrecta,
                        Respondido = false,
                        TiempoRespuesta = 0
                    });
                }

                actuales.Realizados = actuales.ejerciciosCuadros.Count;

                HttpContext.Session.SetString("Ejercicios", JsonSerializer.Serialize(actuales));
                HttpContext.Session.SetString("Respuestas", JsonSerializer.Serialize(respuestas));

                // Redirigir a resultados
                return RedirectToAction("ResultadosCuadrosPractica");
            }
            catch (Exception ex)
            {
                return RedirectToAction("CuadrosForm");
            }
        }

    }
}
