using AnzanMegaArithmetics.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AnzanMegaArithmetics.Controllers
{
    [Authorize]
    public class FlashController : Controller
    {
        [HttpGet]
        public IActionResult FormularioFlash(
            int? CantidadEjercicios,
            string VelocidadPreguntas,
            int? TiempoMeditacion,
            string TipoOperacion,
            string DigitosSuma,
            string DigitosResta,
            int? MinDigitos,
            int? MaxDigitos)
        {
            var modelo = new ConfFlashModel
            {
                CantidadEjercicios = CantidadEjercicios ?? 3,
                VelocidadPreguntas = string.IsNullOrWhiteSpace(VelocidadPreguntas) ? "1.0" : VelocidadPreguntas,
                TiempoMeditacion = TiempoMeditacion ?? 3,
                TipoOperacion = TipoOperacion ?? "suma",
                DigitosSuma = DigitosSuma ?? "",
                DigitosResta = DigitosResta ?? "",
                MinDigitos = MinDigitos ?? 1,
                MaxDigitos = MaxDigitos ?? 2
            };

            return View(modelo);
        }


        [HttpPost]
        public IActionResult Concentracion(ConfFlashModel config)
        {
            config.CantidadEjercicios = Math.Max(1, config.CantidadEjercicios);
            config.TiempoMeditacion = Math.Max(0, config.TiempoMeditacion);

            TempData["CantidadEjercicios"] = config.CantidadEjercicios;
            TempData["MinDigitos"] = config.MinDigitos;
            TempData["MaxDigitos"] = config.MaxDigitos;
            TempData["VelocidadPreguntas"] = config.VelocidadPreguntas;
            TempData["TipoOperacion"] = string.IsNullOrWhiteSpace(config.TipoOperacion) ? "suma" : config.TipoOperacion;
            TempData["TiempoMeditacion"] = config.TiempoMeditacion;
            TempData["EjerciciosRealizados"] = 0;


            TempData["DigitosSuma"] = (config.DigitosSuma ?? "").Replace("\r", "").Replace("\n", "").Trim();
            TempData["DigitosResta"] = (config.DigitosResta ?? "").Replace("\r", "").Replace("\n", "").Trim();

            var modeloFlash = new RFlashModel
            {
                OperacionTexto = "",
                RespuestaCorrecta = 0,
                RespuestaUsuario = 0,
                Respondido = false
            };

            TempData["ResultadoFlash"] = JsonSerializer.Serialize(modeloFlash);

            ViewBag.TiempoMeditacion = config.TiempoMeditacion;
            ViewBag.VelocidadPreguntas = config.VelocidadPreguntas;

            return View();
        }

        [HttpGet]
        public IActionResult EjercicioFlash()
        {
            int totalNumeros = int.Parse(TempData["CantidadEjercicios"]?.ToString() ?? "1");
            int realizados = int.Parse(TempData["EjerciciosRealizados"]?.ToString() ?? "0");
            int minDig = int.Parse(TempData["MinDigitos"]?.ToString() ?? "1");
            int maxDig = int.Parse(TempData["MaxDigitos"]?.ToString() ?? "1");
            string tipoOperacion = TempData["TipoOperacion"]?.ToString() ?? "suma";
            string digitosSuma = TempData["DigitosSuma"]?.ToString() ?? "";
            string digitosResta = TempData["DigitosResta"]?.ToString() ?? "";

            // Verificar si ya se completaron los ejercicios
            if (realizados >= totalNumeros)
                return RedirectToAction("RespuestaFlash");

            var modelo = JsonSerializer.Deserialize<RFlashModel>(TempData["ResultadoFlash"]?.ToString() ?? "{}");

            var digitosValidos = new List<string>();
            if (tipoOperacion == "suma" || tipoOperacion == "ambos")
                digitosValidos.AddRange(digitosSuma.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)));
            if (tipoOperacion == "resta" || tipoOperacion == "ambos")
                digitosValidos.AddRange(digitosResta.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)));

            var rand = new Random();

            string numeroTexto;
            int numero;
            string operador = "+";

            if (realizados == 0)
            {
                // Lógica ajustada para primer número
                int primerDigitos = tipoOperacion switch
                {
                    "resta" => Math.Min(maxDig + 2, 9),
                    "ambos" => Math.Min(maxDig + 1, 9),
                    _ => maxDig
                };

                numeroTexto = GenerarNumero(digitosValidos, primerDigitos, primerDigitos);
                numero = int.Parse(numeroTexto);

                modelo.OperacionTexto = numero.ToString();
                modelo.RespuestaCorrecta = numero;
            }
            else
            {
                operador = tipoOperacion switch
                {
                    "suma" => "+",
                    "resta" => "-",
                    "ambos" => rand.Next(2) == 0 ? "+" : "-",
                    _ => "+"
                };

                int intento = 0;
                do
                {
                    numeroTexto = GenerarNumero(digitosValidos, minDig, maxDig);
                    numero = int.Parse(numeroTexto);
                    intento++;
                    if (intento > 1000)
                        numero = 0;
                }
                while (operador == "-" && modelo.RespuestaCorrecta - numero < 0);

                modelo.OperacionTexto += $" {operador} {numero}";
                modelo.RespuestaCorrecta = operador == "+"
                    ? modelo.RespuestaCorrecta + numero
                    : modelo.RespuestaCorrecta - numero;
            }

            TempData["ResultadoFlash"] = JsonSerializer.Serialize(modelo);
            TempData["CantidadEjercicios"] = totalNumeros;
            TempData["EjerciciosRealizados"] = realizados + 1;
            TempData.Keep();
            TempData.Keep("VelocidadPreguntas");

            ViewBag.Velocidad = float.Parse(TempData["VelocidadPreguntas"]?.ToString() ?? "1", System.Globalization.CultureInfo.InvariantCulture);
            string numeroConSigno = operador == "-" ? "-" + numero.ToString() : numero.ToString();
            ViewBag.NumeroActual = numeroConSigno;

            return View();
        }

        private string GenerarNumero(List<string> digitosPermitidos, int minDig, int maxDig)
        {
            var random = new Random();
            for (int intento = 0; intento < 1000; intento++)
            {
                int longitud = random.Next(minDig, maxDig + 1);
                var numero = new char[longitud];

                for (int i = 0; i < longitud; i++)
                {
                    numero[i] = digitosPermitidos[random.Next(digitosPermitidos.Count)][0];
                }

                string numeroStr = new string(numero);
                if (!numeroStr.StartsWith("0"))
                    return numeroStr;
            }

            return "1";
        }

        [HttpGet]
        public IActionResult RespuestaFlash()
        {
            TempData.Keep();
            return View();
        }

        [HttpPost]
        public IActionResult ResultadoFlash(int respuestaUsuario)
        {
            var resultado = JsonSerializer.Deserialize<RFlashModel>(TempData["ResultadoFlash"]?.ToString() ?? "{}");

            resultado.Respondido = true;
            resultado.RespuestaUsuario = respuestaUsuario;

            TempData["ResultadoFlash"] = JsonSerializer.Serialize(resultado);
            return View("ResultadoFlash", resultado);
        }

        [HttpPost]
        public IActionResult FinalizarDesdeEjercicio()
        {
            TempData.Keep();
            return RedirectToAction("RespuestaFlash");
        }

    }
}
