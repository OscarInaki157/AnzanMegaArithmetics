using AnzanMegaArithmetics.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Text.Json;

namespace AnzanMegaArithmetics.Controllers
{
    [Authorize]
    public class MemorizacionController : Controller
    {
        [HttpGet]
        public IActionResult FormNumeroFigura(int? CantidadEjercicios, int? NumeroDigitos,
                                           string TipoPregunta, string VelocidadPreguntas,
                                           int? TiempoMeditacion)
        {
            ConfNumeroFigura modelo = new ConfNumeroFigura
            {
                CantidadEjercicios = CantidadEjercicios ?? 5,
                NumeroDigitos = NumeroDigitos ?? 1,
                TipoPregunta = TipoPregunta ?? "NumFig",
                VelocidadPreguntas = VelocidadPreguntas ?? "0",
                TiempoMeditacion = TiempoMeditacion ?? 0
            };

            return View(modelo);
        }

        [HttpPost]
        public IActionResult ConcentracionNumeroFigura(ConfNumeroFigura configuracion)
        {
            List<RNumeroFigura> ejercicios = GenerarEjerciciosNF(configuracion);

            EjerciciosNumeroFiguraModel sesion = new EjerciciosNumeroFiguraModel
            {
                ejerciciosLista = ejercicios,
                Realizados = 0
            };

            HttpContext.Session.SetString("ConfigNF", JsonSerializer.Serialize(configuracion));

            HttpContext.Session.SetString("EjerciciosNF", JsonSerializer.Serialize(sesion));


            ViewBag.TiempoMeditacion = configuracion.TiempoMeditacion;
            return View();
        }

        private List<RNumeroFigura> GenerarEjerciciosNF(ConfNumeroFigura configuracion) 
        {
            List<RNumeroFigura> ejercicios = new();
            var Random = new Random();

            for (int i = 0; i < configuracion.CantidadEjercicios; i++) 
            {
                List<int> elementos = GenerarElementosPregunta(configuracion.NumeroDigitos, Random);

                RNumeroFigura ejercicio = new RNumeroFigura
                {
                    NumeroEjercicio = i + 1,
                    TipoPreguntaEjercicio = configuracion.TipoPregunta,
                    ElementosPregunta = elementos,
                    RespuestaCorrecta = elementos,
                    RespuestaUsuario = null,
                    TiempoRespuesta = 0
                };

                ejercicios.Add(ejercicio);
            }

            return ejercicios;

        }

        private List<int> GenerarElementosPregunta(int? numeroDigitos, Random random) 
        {
            List<int> elementos = new();

            for (int i = 0; i < numeroDigitos; i++) 
            {
                elementos.Add(random.Next(0,10));
            }

            return elementos;
        }

        [HttpGet]
        public IActionResult EjercicioNumeroFigura()
        {
            var configJson = HttpContext.Session.GetString("ConfigNF");
            var ejerciciosJson = HttpContext.Session.GetString("EjerciciosNF");


            if (string.IsNullOrEmpty(configJson) || string.IsNullOrEmpty(ejerciciosJson))
            {
                return RedirectToAction("FormNumeroFigura");
            }

            var configuracion = JsonSerializer.Deserialize<ConfNumeroFigura>(configJson);
            var sesionejercicios = JsonSerializer.Deserialize<EjerciciosNumeroFiguraModel>(ejerciciosJson);

            if (sesionejercicios.Realizados >= sesionejercicios.ejerciciosLista.Count)
            {
                return RedirectToAction("ResultadoNF");
            }

            int indice = sesionejercicios.Realizados;
            RNumeroFigura actual = sesionejercicios.ejerciciosLista[indice];

            EjercicioNFViewModel realizar = new EjercicioNFViewModel
            {
                EjercicioActual = actual,
                Configuracion = configuracion,
                NumeroEjercicio = actual.NumeroEjercicio,
                TotalEjercicios = sesionejercicios.ejerciciosLista.Count,
                VelocidadPregunta = double.Parse(configuracion.VelocidadPreguntas)
            };

            return View(realizar);
        }

        [HttpPost]
        public IActionResult EjercicioNumeroFigura([FromForm] string RespuestaUsuario, [FromForm] string TiempoRespuesta)
        {
            var ejerciciosJson = HttpContext.Session.GetString("EjerciciosNF");

            if (string.IsNullOrEmpty(ejerciciosJson))
            {
                return RedirectToAction("FormNumeroFigura");
            }

            var sesionejercicios = JsonSerializer.Deserialize<EjerciciosNumeroFiguraModel>(ejerciciosJson);
            int indice = sesionejercicios.Realizados;

            if (indice < sesionejercicios.ejerciciosLista.Count)
            {
                var actual = sesionejercicios.ejerciciosLista[indice];

                
                actual.RespuestaUsuario = JsonSerializer.Deserialize<List<int>>(RespuestaUsuario);

                // Convertir el tiempo a double
                if (double.TryParse(TiempoRespuesta, out double tiempo))
                {
                    actual.TiempoRespuesta = tiempo;
                }

                sesionejercicios.Realizados++;
            }

            HttpContext.Session.SetString("EjerciciosNF", JsonSerializer.Serialize(sesionejercicios));

            if (sesionejercicios.Realizados >= sesionejercicios.ejerciciosLista.Count)
            {
                return RedirectToAction("ResultadoNF");
            }

            return RedirectToAction("EjercicioNumeroFigura");
        }


        [HttpGet]
        public IActionResult ResultadoNF()
        {
            var ejerciciosJson = HttpContext.Session.GetString("EjerciciosNF");
            var configJson = HttpContext.Session.GetString("ConfigNF");

            if (string.IsNullOrEmpty(ejerciciosJson))
            {
                return RedirectToAction("FormNumeroFigura");
            }

            var sesionEjercicios = JsonSerializer.Deserialize<EjerciciosNumeroFiguraModel>(ejerciciosJson);
            var configuracion = JsonSerializer.Deserialize<ConfNumeroFigura>(configJson);

            // Crear lista de resultados para la vista
            var resultados = new List<ResultadoEjercicioNFViewModel>();

            foreach (var ejercicio in sesionEjercicios.ejerciciosLista)
            {
                var resultado = new ResultadoEjercicioNFViewModel
                {
                    NumeroEjercicio = ejercicio.NumeroEjercicio,
                    ElementosPregunta = ejercicio.ElementosPregunta,
                    RespuestaCorrecta = ejercicio.RespuestaCorrecta,
                    RespuestaUsuario = ejercicio.RespuestaUsuario ?? new List<int> { -1 }, // -1 si no respondió
                    TiempoRespuesta = ejercicio.TiempoRespuesta,
                    EsCorrecto = ejercicio.RespuestaUsuario != null &&
                                ejercicio.RespuestaUsuario.SequenceEqual(ejercicio.RespuestaCorrecta)
                };
                resultados.Add(resultado);
            }

            int total = resultados.Count;
            int correctos = resultados.Count(r => r.EsCorrecto);
            int porcentaje = total > 0 ? (correctos * 100) / total : 0;

            ViewBag.TotalEjercicios = total;
            ViewBag.Correctos = correctos;
            ViewBag.Porcentaje = porcentaje;
            ViewBag.Configuracion = configuracion;

            HttpContext.Session.Remove("EjerciciosNF");


            return View(resultados);
        }

        [HttpPost]
        public IActionResult FinalizarEjercicio([FromForm] string RespuestaUsuario, [FromForm] string TiempoRespuesta)
        {
            var ejerciciosJson = HttpContext.Session.GetString("EjerciciosNF");

            if (string.IsNullOrEmpty(ejerciciosJson))
            {
                return RedirectToAction("FormNumeroFigura");
            }

            var sesionejercicios = JsonSerializer.Deserialize<EjerciciosNumeroFiguraModel>(ejerciciosJson);
            int indice = sesionejercicios.Realizados;

            // Procesar la respuesta actual si hay una
            if (indice < sesionejercicios.ejerciciosLista.Count && !string.IsNullOrEmpty(RespuestaUsuario))
            {
                var actual = sesionejercicios.ejerciciosLista[indice];
                actual.RespuestaUsuario = JsonSerializer.Deserialize<List<int>>(RespuestaUsuario);

                if (double.TryParse(TiempoRespuesta, out double tiempo))
                {
                    actual.TiempoRespuesta = tiempo;
                }

                sesionejercicios.Realizados++;
            }

            // Marcar los ejercicios restantes como no respondidos (-1)
            for (int i = sesionejercicios.Realizados; i < sesionejercicios.ejerciciosLista.Count; i++)
            {
                sesionejercicios.ejerciciosLista[i].RespuestaUsuario = new List<int> { -1 };
                sesionejercicios.ejerciciosLista[i].TiempoRespuesta = 0;
            }

            sesionejercicios.Realizados = sesionejercicios.ejerciciosLista.Count; // Marcar todos como completados

            HttpContext.Session.SetString("EjerciciosNF", JsonSerializer.Serialize(sesionejercicios));

            return RedirectToAction("ResultadoNF");
        }




    }
}
