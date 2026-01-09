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
    }
}
