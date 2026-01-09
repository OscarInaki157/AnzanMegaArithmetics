using AnzanMegaArithmetics.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Text.Json;

namespace AnzanMegaArithmetics.Controllers
{
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
    }
}
