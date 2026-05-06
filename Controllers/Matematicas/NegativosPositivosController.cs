using AnzanMegaArithmetics.Models.MatematicasModels.NegativosPositivos;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AnzanMegaArithmetics.Controllers.Matematicas
{
    public class NegativosPositivosController : Controller
    {
        private static readonly Random _rng = new();

        // ════════════════════════════════════════════════════
        // FORMULARIO
        // ════════════════════════════════════════════════════

        [HttpGet]
        public IActionResult Formulario() =>
            View("FormularioNegativosPositivos", new ConfNegativosPositivosViewModel());

        // ════════════════════════════════════════════════════
        // INICIAR (desde formulario)
        // ════════════════════════════════════════════════════

        [HttpPost]
        public IActionResult Iniciar(ConfNegativosPositivosViewModel config)
        {
            if (!ModelState.IsValid)
                return View("FormularioNegativosPositivos", config);

            config.CantidadEjercicios = Math.Clamp(config.CantidadEjercicios, 1, 100);
            config.MinDigitos = Math.Clamp(config.MinDigitos, 1, 5);
            config.MaxDigitos = Math.Clamp(config.MaxDigitos, config.MinDigitos, 5);

            var estado = new EjercicioNegativosPositivosViewModel
            {
                CantidadEjercicios = config.CantidadEjercicios,
                MinDigitos = config.MinDigitos,
                MaxDigitos = config.MaxDigitos,
                TipoEjercicio = config.TipoEjercicio,
                VelocidadEjercicio = config.VelocidadEjercicio,
                TipoDigitos = config.TipoDigitos,
                EjerciciosRealizados = 0,
                ResultadosJson = "[]",
                EsTutorial = false
            };

            GenerarEjercicio(estado);
            return View("EjercicioNegativosPositivos", estado);
        }

        // ════════════════════════════════════════════════════
        // INICIAR TUTORIAL
        // ════════════════════════════════════════════════════

        [HttpGet]
        public IActionResult IniciarTutorial()
        {
            var estado = new EjercicioNegativosPositivosViewModel
            {
                // Config fija para el tutorial
                CantidadEjercicios = 1,
                MinDigitos = 1,
                MaxDigitos = 1,
                TipoEjercicio = "clasico",
                VelocidadEjercicio = "0",   // sin tiempo
                TipoDigitos = "ambos",
                EjerciciosRealizados = 0,
                ResultadosJson = "[]",
                EsTutorial = true,
                PasoTutorial = 0      // el JS arranca desde paso 0
            };

            GenerarEjercicio(estado);
            return View("EjercicioNegativosPositivos", estado);
        }

        // ════════════════════════════════════════════════════
        // AVANZAR (ejercicio normal)
        // ════════════════════════════════════════════════════

        [HttpPost]
        public IActionResult Avanzar(EjercicioNegativosPositivosViewModel estado)
        {
            ModelState.Clear();

            var lista = Deserializar(estado.ResultadosJson);
            lista.Add(new ResultadoNegativosPositivos
            {
                Operando1 = estado.Operando1,
                Operando2 = estado.Operando2,
                Operacion = estado.Operacion,
                Resultado = estado.Resultado,
                Posicion = estado.Posicion,
                RespuestaCorrecta = estado.RespuestaCorrecta,
                RespuestaUsuario = estado.RespuestaUsuario,
                TiempoRespuesta = estado.TiempoRespuesta
            });

            estado.EjerciciosRealizados++;
            estado.ResultadosJson = JsonSerializer.Serialize(lista);

            // Si es tutorial, al confirmar va directo al dashboard
            if (estado.EsTutorial)
                return RedirectToAction("DashboardMatematicas", "DashboardMatematicas");

            if (estado.EjerciciosRealizados >= estado.CantidadEjercicios)
                return VistaResultado(lista, estado);

            GenerarEjercicio(estado);
            return View("EjercicioNegativosPositivos", estado);
        }

        // ════════════════════════════════════════════════════
        // FINALIZAR (botón Terminar)
        // ════════════════════════════════════════════════════

        [HttpPost]
        public IActionResult Finalizar(EjercicioNegativosPositivosViewModel estado)
        {
            ModelState.Clear();

            // En tutorial, Terminar también regresa al dashboard
            if (estado.EsTutorial)
                return RedirectToAction("DashboardMatematicas", "DashboardMatematicas");

            var lista = Deserializar(estado.ResultadosJson);
            while (lista.Count < estado.CantidadEjercicios)
                lista.Add(new ResultadoNegativosPositivos { RespuestaUsuario = int.MinValue });

            return VistaResultado(lista, estado);
        }

        // ════════════════════════════════════════════════════
        // REGRESAR AL DASHBOARD
        // ════════════════════════════════════════════════════

        [HttpGet]
        public IActionResult RegresarDashboard() =>
            RedirectToAction("DashboardMatematicas", "DashboardMatematicas");

        // ── Helpers ──────────────────────────────────────────

        private void GenerarEjercicio(EjercicioNegativosPositivosViewModel e)
        {
            int a = GenerarNumero(e.MinDigitos, e.MaxDigitos, e.TipoDigitos);
            int b = GenerarNumero(e.MinDigitos, e.MaxDigitos, e.TipoDigitos);
            string op = _rng.Next(2) == 0 ? "+" : "-";
            int res = op == "+" ? a + b : a - b;

            e.Operando1 = a;
            e.Operando2 = b;
            e.Operacion = op;
            e.Resultado = res;

            if (e.TipoEjercicio == "clasico")
            {
                e.Posicion = PosicionIncognita.Resultado;
                e.RespuestaCorrecta = res;
            }
            else
            {
                var pos = (PosicionIncognita)_rng.Next(3);
                e.Posicion = pos;
                e.RespuestaCorrecta = pos switch
                {
                    PosicionIncognita.Operando1 => a,
                    PosicionIncognita.Operando2 => b,
                    _ => res
                };
            }

            e.RespuestaUsuario = int.MinValue;
            e.TiempoRespuesta = 0;
        }

        private static int GenerarNumero(int minDig, int maxDig, string tipoDigitos)
        {
            int digitos = _rng.Next(minDig, maxDig + 1);
            if (digitos == 1)
            {
                return tipoDigitos switch
                {
                    "superiores" => _rng.Next(6, 10),
                    "inferiores" => _rng.Next(1, 6),
                    _ => _rng.Next(1, 10)
                };
            }
            int min = (int)Math.Pow(10, digitos - 1);
            int max = (int)Math.Pow(10, digitos) - 1;
            return _rng.Next(min, max + 1);
        }

        private IActionResult VistaResultado(
            List<ResultadoNegativosPositivos> lista,
            EjercicioNegativosPositivosViewModel e)
        {
            return View("ResultadoNegativosPositivos", new ResultadoFinalNegativosPositivosViewModel
            {
                Resultados = lista,
                CantidadEjercicios = e.CantidadEjercicios,
                TipoEjercicio = e.TipoEjercicio,
                VelocidadEjercicio = e.VelocidadEjercicio,
                TipoDigitos = e.TipoDigitos,
                MinDigitos = e.MinDigitos,
                MaxDigitos = e.MaxDigitos
            });
        }

        private static List<ResultadoNegativosPositivos> Deserializar(string json)
        {
            if (string.IsNullOrEmpty(json)) return new();
            return JsonSerializer.Deserialize<List<ResultadoNegativosPositivos>>(json) ?? new();
        }
    }
}
