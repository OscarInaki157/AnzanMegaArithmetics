using AnzanMegaArithmetics.Models;
using AnzanMegaArithmetics.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AnzanMegaArithmetics.Controllers
{
    [Authorize]
    public class RutasMemoriaController : Controller
    {
        private readonly IPruebasDBService _pruebasDBService;

        public RutasMemoriaController(IPruebasDBService pruebasDBService)
        {
            this._pruebasDBService = pruebasDBService;
        }

        [HttpGet]
        public IActionResult FormRutasMemoria(ConfRutasModel configGuardada)
        {
            var userId = HttpContext.Session.GetInt32("Id_Usuario");

            if (userId == null || userId == 0)
            {
                return RedirectToAction("Inicio", "Inicio");
            }


            if (configGuardada != null && configGuardada.CantidadEjercicios > 0)
            {

                return View(configGuardada);
            }


            var modeloPorDefecto = new ConfRutasModel
            {
                CantidadEjercicios = 20,
                ValorInicial = 1,
                ValorFinal = 10,
                TiempoMeditacion = 3,
                VelocidadPreguntas = "0",
                Modalidad = "Secuencial",
                CategoriaEjercicios = "Lista básica",
                TipoPregunta = "Imagen",
                TipoRespuesta = "Nombre"
            };

            return View(modeloPorDefecto);
        }

        [HttpPost]
        public IActionResult ConcentracionRutas(ConfRutasModel config)
        {
            //Generar ejercicios
            List<RRutaMModel> ejercicios = GenerarEjerciciosRM(config);

            SesionERutasMModel sesion = new SesionERutasMModel
            {
                ListaEjercicios = ejercicios,
                Realizados = 0
            };

            HttpContext.Session.SetString("ConfigRutas", JsonSerializer.Serialize(config));
            HttpContext.Session.SetString("EjerciciosRutas", JsonSerializer.Serialize(sesion));

            ViewBag.TiempoMeditacion = config.TiempoMeditacion;
            return View();
        }

        [HttpGet]
        public IActionResult EjercicioRutasMemoria()
        {
            var configJson = HttpContext.Session.GetString("ConfigRutas");
            var ejerciciosJson = HttpContext.Session.GetString("EjerciciosRutas");

            if (string.IsNullOrEmpty(configJson) || string.IsNullOrEmpty(ejerciciosJson))
            {
                return RedirectToAction("FormRutasMemoria");
            }

            ConfRutasModel configuracion = JsonSerializer.Deserialize<ConfRutasModel>(configJson);
            SesionERutasMModel sesionEjercicios = JsonSerializer.Deserialize<SesionERutasMModel>(ejerciciosJson);

            if (sesionEjercicios.Realizados >= sesionEjercicios.ListaEjercicios.Count)
            {
                return RedirectToAction("ResultadoRutas");
            }

            int indice = sesionEjercicios.Realizados;
            RRutaMModel actual = sesionEjercicios.ListaEjercicios[indice];

            EjercicioRMViewModel ejercicio = new EjercicioRMViewModel
            {
                EjercicioActual = actual,
                Configuracion = configuracion,
                NumeroEjercicio = actual.Id_Ejercicio,
                TotalEjercicios = sesionEjercicios.ListaEjercicios.Count,
                VelocidadPregunta = double.Parse(configuracion.VelocidadPreguntas, System.Globalization.CultureInfo.InvariantCulture)
            };


            return View(ejercicio);
        }

        [HttpPost]
        public IActionResult EjercicioRutasMemoria([FromForm] string RespuestaUsuario, [FromForm] string TiempoRespuesta)
        {
            var ejerciciosJson = HttpContext.Session.GetString("EjerciciosRutas");
            var configJson = HttpContext.Session.GetString("ConfigRutas");

            if (string.IsNullOrEmpty(ejerciciosJson) || string.IsNullOrEmpty(configJson))
            {
                return RedirectToAction("FormRutasMemoria");
            }

            var sesionEjercicios = JsonSerializer.Deserialize<SesionERutasMModel>(ejerciciosJson);
            var configuracion = JsonSerializer.Deserialize<ConfRutasModel>(configJson);
            int indice = sesionEjercicios.Realizados;

            if (indice < sesionEjercicios.ListaEjercicios.Count)
            {
                var actual = sesionEjercicios.ListaEjercicios[indice];

                List<int> idsSeleccionados = new List<int>();
                if (!string.IsNullOrEmpty(RespuestaUsuario))
                {
                    idsSeleccionados = JsonSerializer.Deserialize<List<int>>(RespuestaUsuario);
                }

                actual.RespuestaSeleccionada = actual.ListaOpciones
                    .Where(o => idsSeleccionados.Contains(o.Numero))
                    .ToList();

                int respuestasEsperadas = (configuracion.TipoRespuesta.Contains(" e ") || configuracion.TipoRespuesta.Contains(" y ")) ? 2 : 1;

                bool respondioAlgo = idsSeleccionados.Count > 0;
                bool cantidadCorrecta = idsSeleccionados.Count == respuestasEsperadas;
                bool sonTodosCorrectos = idsSeleccionados.All(id => id == actual.ElementoPregunta.Numero);


                actual.EsCorrecta = respondioAlgo && cantidadCorrecta && sonTodosCorrectos;

                if (double.TryParse(TiempoRespuesta, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double tiempo))
                {
                    actual.TiempoRespuesta = tiempo;
                }

                sesionEjercicios.Realizados++;
            }

            HttpContext.Session.SetString("EjerciciosRutas", JsonSerializer.Serialize(sesionEjercicios));

            if (sesionEjercicios.Realizados >= sesionEjercicios.ListaEjercicios.Count)
            {
                return RedirectToAction("ResultadoRutas");
            }

            return RedirectToAction("EjercicioRutasMemoria");
        }

        [HttpPost]
        public IActionResult FinalizarEjercicio()
        {
            var ejerciciosJson = HttpContext.Session.GetString("EjerciciosRutas");

            if (string.IsNullOrEmpty(ejerciciosJson))
            {
                return RedirectToAction("FormRutasMemoria");
            }

            var sesionEjercicios = JsonSerializer.Deserialize<SesionERutasMModel>(ejerciciosJson);

            int realizados = sesionEjercicios.Realizados;

            for (int i = realizados; i < sesionEjercicios.ListaEjercicios.Count; i++)
            {
                var ejercicioPendiente = sesionEjercicios.ListaEjercicios[i];

                ejercicioPendiente.EsCorrecta = false;
                ejercicioPendiente.TiempoRespuesta = 0;
                ejercicioPendiente.RespuestaSeleccionada = new List<OERutaMModel>();
            }


            sesionEjercicios.Realizados = sesionEjercicios.ListaEjercicios.Count;

            HttpContext.Session.SetString("EjerciciosRutas", JsonSerializer.Serialize(sesionEjercicios));

            return RedirectToAction("ResultadoRutas");
        }

        [HttpGet]
        public IActionResult ResultadoRutas()
        {
            var userId = HttpContext.Session.GetInt32("Id_Usuario");

            if (userId == null || userId == 0)
            {
                return RedirectToAction("Inicio", "Inicio");
            }

            var ejerciciosJson = HttpContext.Session.GetString("EjerciciosRutas");
            var configJson = HttpContext.Session.GetString("ConfigRutas");

            if (string.IsNullOrEmpty(ejerciciosJson))
            {
                return RedirectToAction("FormRutasMemoria");
            }

            var sesionEjercicios = JsonSerializer.Deserialize<SesionERutasMModel>(ejerciciosJson);
            var configuracion = JsonSerializer.Deserialize<ConfRutasModel>(configJson);
            double tiempoTotal = 0;


            var resultados = new List<RRutaMModel>();

            foreach (var ejercicio in sesionEjercicios.ListaEjercicios)
            {

                resultados.Add(ejercicio);
                tiempoTotal += ejercicio.TiempoRespuesta;
            }

            int total = resultados.Count;
            int correctos = resultados.Count(r => r.EsCorrecta);
            int porcentaje = total > 0 ? (correctos * 100) / total : 0;

            ViewBag.TotalEjercicios = total;
            ViewBag.Correctos = correctos;
            ViewBag.Porcentaje = porcentaje;
            ViewBag.Configuracion = configuracion;


            HttpContext.Session.Remove("EjerciciosRutas");

            PruebasDBModel result = new PruebasDBModel
            {
                Id_Usuario = userId.Value,
                Total_Preguntas = total,
                Tiempo = TimeSpan.FromSeconds(tiempoTotal),
                Respuestas_Correctas = correctos,
                Fecha = DateTime.Now,
                ExperienciaAdquirida = porcentaje,
                Tipo_Prueba = "Rutas de Memoria",
                Configuracion= configJson ?? "No se pudo recuperar la configuración del servidor"
            };

            bool InsertarPrueba = _pruebasDBService.GuardarPrueba(result);

            return View(resultados);
        }

        private List<RRutaMModel> GenerarEjerciciosRM(ConfRutasModel config)
        {
            List<RRutaMModel> listaEjercicios = new List<RRutaMModel>();
            var random = new Random();

            Dictionary<int, string> nombresDic;
            Dictionary<int, string> imagenesDic;

            if (config.CategoriaEjercicios.Contains("sica"))
            {
                nombresDic = NombresListaBasica;
                imagenesDic = ImgListaBasica;
            }
            else
            {
                nombresDic = NombresViajeAmerica;
                imagenesDic = ImgViajeAmerica;
            }

            //ValorInicial al ValorFinal
            List<int> llavesRango = nombresDic.Keys
                .Where(k => k >= config.ValorInicial && k <= config.ValorFinal)
                .ToList();

            //modalidad Secuencial o Aleatorio
            if (config.Modalidad.Contains("atorio"))
            {
                // Desordenamos aleatoriamente
                llavesRango = llavesRango.OrderBy(x => random.Next()).ToList();
            }
            else
            {
                //orden secuencial
                llavesRango = llavesRango.OrderBy(x => x).ToList();
            }

            for (int i = 0; i < llavesRango.Count; i++)
            {
                int llaveActual = llavesRango[i];

                OERutaMModel elementoPregunta = new OERutaMModel
                {
                    Numero = llaveActual,
                    Nombre = nombresDic[llaveActual],
                    ImagenURL = imagenesDic[llaveActual]
                };

                RRutaMModel ejercicio = new RRutaMModel
                {
                    Id_Ejercicio = i + 1,
                    ElementoPregunta = elementoPregunta,
                    ListaOpciones = GenerarOpciones(llaveActual, nombresDic, imagenesDic, random),
                    TiempoRespuesta = 0,
                    EsCorrecta = false,
                    RespuestaSeleccionada = new List<OERutaMModel>()
                };

                listaEjercicios.Add(ejercicio);
            }

            return listaEjercicios;
        }

        private List<OERutaMModel> GenerarOpciones(int llaveCorrecta, Dictionary<int, string> nombresDic, Dictionary<int, string> imagenesDic, Random random)
        {
            List<OERutaMModel> opciones = new List<OERutaMModel>();

            //respuesta correcta
            opciones.Add(new OERutaMModel
            {
                Numero = llaveCorrecta,
                Nombre = nombresDic[llaveCorrecta],
                ImagenURL = imagenesDic[llaveCorrecta]
            });


            List<int> llavesDistractores = nombresDic.Keys
                .Where(k => k != llaveCorrecta)
                .OrderBy(x => random.Next())
                .Take(7)
                .ToList();

            foreach (int distractor in llavesDistractores)
            {
                opciones.Add(new OERutaMModel
                {
                    Numero = distractor,
                    Nombre = nombresDic[distractor],
                    ImagenURL = imagenesDic[distractor]
                });
            }

            return opciones.OrderBy(x => random.Next()).ToList();
        }

        private readonly Dictionary<int, string> NombresListaBasica = new Dictionary<int, string>
        {
            { 1, "Torre Ajedrez" },
            { 2, "Ojos" },
            { 3, "Medalla de bronce" },
            { 4, "Jeep" },
            { 5, "Mano" },
            { 6, "Pistola" },
            { 7, "Arcoíris" },
            { 8, "Bicicleta" },
            { 9, "Gato" },
            { 10, "Bate y pelota" },
            { 11, "Torres Gemelas"},
            { 12, "Reloj" },
            { 13, "Jason" },
            { 14, "Anillo" },
            { 15, "Billar" },
            { 16, "Cancha de tenis" },
            { 17, "Revista" },
            { 18, "Urna" },
            { 19, "Covid" },
            { 20, "Dientes" }
        };

        private readonly Dictionary<int, string> NombresViajeAmerica = new Dictionary<int, string>
        {
            { 1, "Casa de santa Polo Norte" },
            { 2, "Cataratas del Niagara Canadá" },
            { 3, "Estatua de la libertad USA" },
            { 4, "Hollywood USA" },
            { 5, "El Arco Baja California" },
            { 6, "Ángel independencia CDMX" },
            { 7, "Chichen Itzá Yucatán" },
            { 8, "Capitolio Cuba" },
            { 9, "Volcán de Fuego Guatemala" },
            { 10, "Canal Panamá" },
            { 11, "Casa Simón Bolívar Venezuela" },
            { 12, "Peñón de Guatepé Colombia" },
            { 13, "Mitad del mundo Ecuador" },
            { 14, "Machu Picchu Perú" },
            { 15, "Carnaval de Brasil" },
            { 16, "El Cristo Brasil" },
            { 17, "Salar de Uyuni Bolivia" },
            { 18, "Mano del desierto Chile" },
            { 19, "La Bombonera Argentina" },
            { 20, "Los glaciares Argentina" }
        };

        private readonly Dictionary<int, string> ImgListaBasica = new Dictionary<int, string>
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

        private readonly Dictionary<int, string> ImgViajeAmerica = new Dictionary<int, string>
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

        [HttpPost]
        public IActionResult ConcentracionGaleria(ConfRutasModel config)
        {
            HttpContext.Session.SetString("ConfigGaleria", JsonSerializer.Serialize(config));

            ViewBag.TiempoMeditacion = config.TiempoMeditacion;
            return View();
        }

        [HttpGet]
        public IActionResult EjercicioDidactico()
        {
            var configJson = HttpContext.Session.GetString("ConfigGaleria");

            if (string.IsNullOrEmpty(configJson))
            {
                return RedirectToAction("FormRutasMemoria");
            }

            ConfRutasModel config = JsonSerializer.Deserialize<ConfRutasModel>(configJson);
            List<OERutaMModel> galeria = new List<OERutaMModel>();

            Dictionary<int, string> nombresDic;
            Dictionary<int, string> imagenesDic;

            if (config.CategoriaEjercicios.Contains("sica"))
            {
                nombresDic = NombresListaBasica;
                imagenesDic = ImgListaBasica;
            }
            else
            {
                nombresDic = NombresViajeAmerica;
                imagenesDic = ImgViajeAmerica;
            }

            List<int> llavesRango = nombresDic.Keys
                .Where(k => k >= 1 && k <= config.CantidadEjercicios)
                .OrderBy(x => x)
                .ToList();

            foreach (int llave in llavesRango)
            {
                galeria.Add(new OERutaMModel
                {
                    Numero = llave,
                    Nombre = nombresDic[llave],
                    ImagenURL = imagenesDic[llave]
                });
            }

            return View(galeria);
        }

        [HttpPost]
        public IActionResult FinalizarDidactico() 
        {
            return RedirectToAction("FormRutasMemoria");
        }
    }
}
