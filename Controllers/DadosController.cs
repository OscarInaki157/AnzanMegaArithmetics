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



    }
}
