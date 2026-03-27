using AnzanMegaArithmetics.Models;
using AnzanMegaArithmetics.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json;

namespace AnzanMegaArithmetics.Controllers
{
    [Authorize]
    public class MemoriaFlashController : Controller
    {
        private readonly IPruebasDBService _pruebasDBService;

        public MemoriaFlashController(IPruebasDBService pruebasDBService)
        {
            this._pruebasDBService = pruebasDBService;
        }

        [HttpGet]
        public IActionResult LimpiarYDashboard()
        {
            HttpContext.Session.Remove("ConfigMemoriaFlash");
            HttpContext.Session.Remove("SesionMemoriaFlash");
            HttpContext.Session.Remove("ResultadosMemoriaFlash");

            return RedirectToAction("Memorizacion","Dashboard");
        }

        [HttpGet]
        public IActionResult FormularioMemoriaFlash() 
        {
            var userId = HttpContext.Session.GetInt32("Id_Usuario");

            if (userId == null || userId == 0) 
            {
                return RedirectToAction("Inicio", "Inicio");
            }
            
            var configStr = HttpContext.Session.GetString("ConfigMemoriaFlash");

            ConfMemoriaFlashModel config;

            if (!string.IsNullOrEmpty(configStr))
            {
                try
                {
                    config = JsonSerializer.Deserialize<ConfMemoriaFlashModel>(configStr);
                }
                catch
                {
                    config = ObtenerConfiguracionPorDefecto();
                }
            }
            else 
            {
                config = ObtenerConfiguracionPorDefecto();
            }

            return View(config);
        }

        private ConfMemoriaFlashModel ObtenerConfiguracionPorDefecto() 
        {
            return new ConfMemoriaFlashModel
            {
                CantidadEjercicios = 20,
                VelocidadPreguntas = "1",
                CategoriaEjercicios = "Lista básica",
                TipoPreguntas = "Números",
                DigitosEjercicios = 1,
                MostrarParejas = "No",
                TiempoMeditacion = 3,
                ActivarSonido = true,
                ColorA = "color1",
                ColorB = "color2"
            };
        }

        [HttpPost]
        public IActionResult ConcentracionMemoriaFlash(ConfMemoriaFlashModel config) 
        {
            //generar ejercicios
            SesionMemoriaFlashModel sesion = new SesionMemoriaFlashModel
            {
                Configuracion = config,
                Ejercicios = GenerarEjercicios(config)
            };

            //guardar en sesion
            string sesionStr = JsonSerializer.Serialize(sesion);
            HttpContext.Session.SetString("SesionMemoriaFlash", sesionStr);

            //datos concentracion
            ViewBag.TiempoMeditacion = config.TiempoMeditacion;
            ViewBag.VelocidadPreguntas = config.VelocidadPreguntas;
            return View();
        }

        private List<EjercicioMemoriaFlashModel> GenerarEjercicios(ConfMemoriaFlashModel config) 
        {
            List<EjercicioMemoriaFlashModel> ejercicios = new();
            Random random = new();

            //si no se van a mostrar parejas, no es necesario obtener el mapa
            Dictionary<int, string> mapaParejas = null;

            if (config.MostrarParejas != "No")
            {
                mapaParejas = ObtenerMapaParejas(config.CategoriaEjercicios);
            }

            int maxParejas = mapaParejas?.Count ?? 0;

            for (int i = 1; i <= config.CantidadEjercicios; i++) 
            {
                int ejercicioNumeroCompleto = 0;
                List<DigitoEjercicioMFModel> digitosEjercicio = new();
                string valorNumericoStr = "";

                for (int d = 0; d < config.DigitosEjercicios; d++) 
                {
                    int digito = random.Next(0, 10);
                    valorNumericoStr += digito.ToString();

                    digitosEjercicio.Add(new DigitoEjercicioMFModel 
                    {
                        DigitoNumerico = digito,
                        DigitoImagenRuta = FigurasPorDigito[digito]
                    });
                }

                int.TryParse(valorNumericoStr, out ejercicioNumeroCompleto);

                string colorClase = (i % 2 != 0) ? config.ColorA : config.ColorB;
                string parejaMemoriaRuta = null;


                //si mostrar pareja se selecciona como numero, solo obtener el id
                if (config.MostrarParejas == "Número")
                {
                    parejaMemoriaRuta = $"ID_{i}";
                }
                //si lapareja de memoria es imagen, obtener la ruta
                else if (config.MostrarParejas == "Imagen" && mapaParejas != null && maxParejas > 0)
                {
                    int parejaId = (i % maxParejas == 0) ? maxParejas : i % maxParejas;
                    parejaMemoriaRuta = mapaParejas[parejaId];
                }

                ejercicios.Add(new EjercicioMemoriaFlashModel
                {
                    Id_Ejercicio = i,
                    EjercicioNumero = ejercicioNumeroCompleto,
                    Digitos = digitosEjercicio,
                    ColorClase = colorClase,
                    ParejaMemoriaRuta = parejaMemoriaRuta
                });

            }

            return ejercicios;
        }

        private readonly Dictionary<int, string> FigurasPorDigito = new Dictionary<int, string> 
        {
            { 0, "/Content/Images/0.png"},
            { 1, "/Content/Images/1.png"},
            { 2, "/Content/Images/2.png"},
            { 3, "/Content/Images/3.png"},
            { 4, "/Content/Images/4.png"},
            { 5, "/Content/Images/5.png"},
            { 6, "/Content/Images/6.png"},
            { 7, "/Content/Images/7.png"},
            { 8, "/Content/Images/8.png"},
            { 9, "/Content/Images/9.png"}
        };

        private readonly Dictionary<int, string> ParejasListaBasica = new Dictionary<int, string> 
        {
            { 1, "/Content/Images/ListaBasica/1.png"},
            { 2, "/Content/Images/ListaBasica/2.png"},
            { 3, "/Content/Images/ListaBasica/3.png"},
            { 4, "/Content/Images/ListaBasica/4.png"},
            { 5, "/Content/Images/ListaBasica/5.png"},
            { 6, "/Content/Images/ListaBasica/6.png"},
            { 7, "/Content/Images/ListaBasica/7.png"},
            { 8, "/Content/Images/ListaBasica/8.png"},
            { 9, "/Content/Images/ListaBasica/9.png"},
            { 10, "/Content/Images/ListaBasica/10.png"},
            { 11, "/Content/Images/ListaBasica/11.png"},
            { 12, "/Content/Images/ListaBasica/12.png"},
            { 13, "/Content/Images/ListaBasica/13.png"},
            { 14, "/Content/Images/ListaBasica/14.png"},
            { 15, "/Content/Images/ListaBasica/15.png"},
            { 16, "/Content/Images/ListaBasica/16.png"},
            { 17, "/Content/Images/ListaBasica/17.png"},
            { 18, "/Content/Images/ListaBasica/18.png"},
            { 19, "/Content/Images/ListaBasica/19.png"},
            { 20, "/Content/Images/ListaBasica/20.png"}
        };

        private readonly Dictionary<int, string> ParejasViajeAmerica = new Dictionary<int, string>
        {
            { 1, "/Content/Images/ViajeAmerica/1.png"},
            { 2, "/Content/Images/ViajeAmerica/2.png"},
            { 3, "/Content/Images/ViajeAmerica/3.png"},
            { 4, "/Content/Images/ViajeAmerica/4.png"},
            { 5, "/Content/Images/ViajeAmerica/5.png"},
            { 6, "/Content/Images/ViajeAmerica/6.png"},
            { 7, "/Content/Images/ViajeAmerica/7.png"},
            { 8, "/Content/Images/ViajeAmerica/8.png"},
            { 9, "/Content/Images/ViajeAmerica/9.png"},
            { 10, "/Content/Images/ViajeAmerica/10.png"},
            { 11, "/Content/Images/ViajeAmerica/11.png"},
            { 12, "/Content/Images/ViajeAmerica/12.png"},
            { 13, "/Content/Images/ViajeAmerica/13.png"},
            { 14, "/Content/Images/ViajeAmerica/14.png"},
            { 15, "/Content/Images/ViajeAmerica/15.png"},
            { 16, "/Content/Images/ViajeAmerica/16.png"},
            { 17, "/Content/Images/ViajeAmerica/17.png"},
            { 18, "/Content/Images/ViajeAmerica/18.png"},
            { 19, "/Content/Images/ViajeAmerica/19.png"},
            { 20, "/Content/Images/ViajeAmerica/20.png"}
        };

        private Dictionary<int, string> ObtenerMapaParejas(string categoria) 
        {
            switch (categoria) 
            {
                case "Lista básica":
                    return ParejasListaBasica;
                case "Viaje a América":
                    return ParejasViajeAmerica;
                default:
                    return ParejasListaBasica;
            }
        }

        [HttpGet]
        public IActionResult EjercicioMemoriaFlash() 
        {
            string sesionStr = HttpContext.Session.GetString("SesionMemoriaFlash");

            if (string.IsNullOrEmpty(sesionStr))
            {
                return RedirectToAction("FormularioMemoriaFlash");
            }

            try 
            {
                SesionMemoriaFlashModel sesionDeserializada = JsonSerializer.Deserialize<SesionMemoriaFlashModel>(sesionStr);
                return View(sesionDeserializada);
            } catch 
            {
                HttpContext.Session.Remove("SesionMemoriaFlash");
                HttpContext.Session.Remove("ConfigMemoriaFlash");
                return RedirectToAction("FormularioMemoriaFlash");
            }
        }

        [HttpGet]
        public IActionResult RespuestaMemoriaFlash()
        {
            string sesionStr = HttpContext.Session.GetString("SesionMemoriaFlash");

            if (string.IsNullOrEmpty(sesionStr))
            {
                return RedirectToAction("FormularioMemoriaFlash");
            }

            try
            {
                SesionMemoriaFlashModel sesionDeserializada = JsonSerializer.Deserialize<SesionMemoriaFlashModel>(sesionStr);
                ViewData["FigurasPorDigito"] = FigurasPorDigito;
                return View(sesionDeserializada);
            }
            catch (Exception ex)
            {
                HttpContext.Session.Remove("SesionMemoriaFlash");
                return RedirectToAction("FormularioMemoriaFlash");
            }
        }

        [HttpPost]
        public IActionResult CalificarRespuestasFlash(RespuestasSesionMFModel respuestasModel)
        {
            
            string sesionStr = HttpContext.Session.GetString("SesionMemoriaFlash");

            if (string.IsNullOrEmpty(sesionStr) || respuestasModel?.Respuestas == null || respuestasModel.Respuestas.Count == 0)
            {
                
                return RedirectToAction("FormularioMemoriaFlash");
            }

            SesionMemoriaFlashModel sesionOriginal;
            try
            {
                sesionOriginal = JsonSerializer.Deserialize<SesionMemoriaFlashModel>(sesionStr);
            }
            catch
            {
                
                return RedirectToAction("FormularioMemoriaFlash");
            }

            
            int totalAciertos = 0;
            List<ResultadoEjercicioMFModel> resultadosDetalle = new List<ResultadoEjercicioMFModel>();

            
            var ejercicioMap = sesionOriginal.Ejercicios.ToDictionary(e => e.Id_Ejercicio, e => e);

            // 3. Calificar cada respuesta del usuario
            foreach (var respuestaUsuario in respuestasModel.Respuestas)
            {
                if (ejercicioMap.TryGetValue(respuestaUsuario.Id_Ejercicio, out var ejercicioOriginal))
                {
                    // La respuesta correcta es EjercicioNumero
                    bool respondido = respuestaUsuario.RespuestaUsuario != -1;
                    bool esCorrecto = false;

                    if (respondido)
                    {
                        esCorrecto = (ejercicioOriginal.EjercicioNumero == respuestaUsuario.RespuestaUsuario);
                    }

                    if (esCorrecto)
                    {
                        totalAciertos++;
                    }

                    resultadosDetalle.Add(new ResultadoEjercicioMFModel
                    {
                        
                        Id_Ejercicio = ejercicioOriginal.Id_Ejercicio,
                        EjercicioNumero = ejercicioOriginal.EjercicioNumero,
                        Digitos = ejercicioOriginal.Digitos,
                        ColorClase = ejercicioOriginal.ColorClase,
                        ParejaMemoriaRuta = ejercicioOriginal.ParejaMemoriaRuta,

                        RespuestaUsuario = respuestaUsuario.RespuestaUsuario,
                        EsCorrecto = esCorrecto,
                        Respondido = respondido
                    });
                }
            }

            int totalEjercicios = sesionOriginal.Ejercicios.Count;
            int porcentajeGlobal = totalEjercicios > 0 ? (int)(((double)totalAciertos / totalEjercicios) * 100) : 0;
            double velocidadPreguntas = double.Parse(sesionOriginal.Configuracion.VelocidadPreguntas);
            double tiempoTotal = totalEjercicios * velocidadPreguntas;
            string tipoPrueba = "Memoria Flash " + sesionOriginal.Configuracion.DigitosEjercicios + " digitos";
            var userId = HttpContext.Session.GetInt32("Id_Usuario");

            string configJson = JsonSerializer.Serialize(sesionOriginal.Configuracion);

            // Guardar resultados en la base de datos
            PruebasDBModel results = new PruebasDBModel
            {
                Id_Usuario = userId.Value,
                Total_Preguntas = totalEjercicios,
                Tiempo = TimeSpan.FromSeconds(tiempoTotal),
                Respuestas_Correctas = totalAciertos,
                Fecha = DateTime.Now,
                ExperienciaAdquirida = porcentajeGlobal,
                Tipo_Prueba = tipoPrueba,
                Configuracion = configJson
            };

            bool InsertarPrueba = _pruebasDBService.GuardarPrueba(results);

            ResultadosSesionMFModel resultadosFinales = new ResultadosSesionMFModel
            {
                Configuracion = sesionOriginal.Configuracion,
                TotalEjercicios = sesionOriginal.Ejercicios.Count,
                TotalAciertos = totalAciertos,
                ResultadosEjercicios = resultadosDetalle
            };

            
            string resultadosStr = JsonSerializer.Serialize(resultadosFinales);
            HttpContext.Session.SetString("ResultadosMemoriaFlash", resultadosStr);

            
            return RedirectToAction("ResultadosMemoriaFlash");
        }

        [HttpGet]
        public IActionResult ResultadosMemoriaFlash()
        {
            string resultadosStr = HttpContext.Session.GetString("ResultadosMemoriaFlash");

            if (string.IsNullOrEmpty(resultadosStr))
            {
                return RedirectToAction("FormularioMemoriaFlash");
            }

            ResultadosSesionMFModel resultados;
            try
            {
                resultados = JsonSerializer.Deserialize<ResultadosSesionMFModel>(resultadosStr);
                string configStr = JsonSerializer.Serialize(resultados.Configuracion);
                HttpContext.Session.SetString("ConfigMemoriaFlash", configStr);
            }
            catch (Exception ex)
            {
                HttpContext.Session.Remove("ResultadosMemoriaFlash");
                return RedirectToAction("FormularioMemoriaFlash");
            }

            return View(resultados);
        }


        [HttpPost]
        public IActionResult RepetirMemoriaFlash()
        {
            string resultadosStr = HttpContext.Session.GetString("ResultadosMemoriaFlash");

            if (string.IsNullOrEmpty(resultadosStr))
            {
                return RedirectToAction("FormularioMemoriaFlash");
            }

            try
            {
                ResultadosSesionMFModel resultadosAnteriores = JsonSerializer.Deserialize<ResultadosSesionMFModel>(resultadosStr);
                ConfMemoriaFlashModel config = resultadosAnteriores.Configuracion;

                SesionMemoriaFlashModel nuevaSesion = new SesionMemoriaFlashModel
                {
                    Configuracion = config,
                    Ejercicios = GenerarEjercicios(config)
                };

                string sesionStr = JsonSerializer.Serialize(nuevaSesion);
                HttpContext.Session.SetString("SesionMemoriaFlash", sesionStr);

                HttpContext.Session.Remove("ResultadosMemoriaFlash");

                ViewBag.TiempoMeditacion = config.TiempoMeditacion;
                ViewBag.VelocidadPreguntas = config.VelocidadPreguntas;

                return View("ConcentracionMemoriaFlash");
            }
            catch (Exception)
            {
                HttpContext.Session.Remove("ResultadosMemoriaFlash");
                return RedirectToAction("FormularioMemoriaFlash");
            }
        }




    }
}
