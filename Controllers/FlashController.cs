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
        public IActionResult LimpiarFlashYDashboard()
        {
            HttpContext.Session.Remove("ConfFlash");
            HttpContext.Session.Remove("HistorialFlash");
            HttpContext.Session.Remove("ResultadoFlash");
            HttpContext.Session.Remove("SecuenciaNumeros");

            return RedirectToAction("Dashboard", "Dashboard");
        }

        [HttpGet]
        public IActionResult FormularioFlash()
        {
            var configStr = HttpContext.Session.GetString("ConfFlash");

            ConfFlashModel modelo;

            if (!string.IsNullOrEmpty(configStr))
            {
                try
                {
                    modelo = JsonSerializer.Deserialize<ConfFlashModel>(configStr);
                }
                catch
                {
                    modelo = ObtenerConfiguracionPorDefecto();
                }
            }
            else
            {
                modelo = ObtenerConfiguracionPorDefecto();
            }

            return View(modelo);
        }

        private ConfFlashModel ObtenerConfiguracionPorDefecto()
        {
            return new ConfFlashModel
            {
                CantidadEjercicios = 5,
                VelocidadPreguntas = "1.0",
                TiempoMeditacion = 3,
                TipoOperacion = "suma",
                DigitosSuma = "1,2,3,4,5,6,7,8,9",
                DigitosResta = "1,2,3,4,5,6,7,8,9",
                MinDigitos = 1,
                MaxDigitos = 1
            };
        }


        [HttpGet]
        public IActionResult Concentracion()
        {
            var configStr = HttpContext.Session.GetString("ConfFlash");
            if (string.IsNullOrEmpty(configStr))
                return RedirectToAction("FormularioFlash");

            var config = JsonSerializer.Deserialize<ConfFlashModel>(configStr);
            return Concentracion(config); // reutiliza lógica POST
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
            TempData["TipoOperacion"] = config.TipoOperacion ?? "suma";
            TempData["TiempoMeditacion"] = config.TiempoMeditacion;

            var digitosSuma = (config.DigitosSuma ?? "").Replace("\r", "").Replace("\n", "").Trim();
            var digitosResta = (config.DigitosResta ?? "").Replace("\r", "").Replace("\n", "").Trim();

            TempData["DigitosSuma"] = digitosSuma;
            TempData["DigitosResta"] = digitosResta;

            var tipoOperacion = config.TipoOperacion ?? "suma";
            var cantidad = config.CantidadEjercicios;
            var minDig = config.MinDigitos;
            var maxDig = config.MaxDigitos;

            var digitosValidos = new List<string>();
            if (tipoOperacion == "suma" || tipoOperacion == "ambos")
                digitosValidos.AddRange(digitosSuma.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)));
            if (tipoOperacion == "resta" || tipoOperacion == "ambos")
                digitosValidos.AddRange(digitosResta.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)));

            var rand = new Random();
            var numeros = new List<int>();
            var secuencia = new List<string>();

            // Detectar si hay restas
            bool incluyeResta = tipoOperacion == "resta" || tipoOperacion == "ambos";

            // Generar el primer número con más dígitos si hay resta
            int maxDigPrimerNumero = incluyeResta ? Math.Min(maxDig + 2, 9) : maxDig;
            string primerNumeroTexto = GenerarNumero(digitosValidos, minDig, maxDigPrimerNumero);
            int acumulado = int.Parse(primerNumeroTexto);
            numeros.Add(acumulado);
            secuencia.Add(acumulado.ToString());


            // Generar los siguientes con operaciones
            for (int i = 1; i < cantidad; i++)
            {
                string operador = tipoOperacion switch
                {
                    "suma" => "+",
                    "resta" => "-",
                    "ambos" => rand.Next(2) == 0 ? "+" : "-",
                    _ => "+"
                };

                int intento = 0;
                string nuevoTexto;
                int nuevoNumero;

                do
                {
                    nuevoTexto = GenerarNumero(digitosValidos, minDig, maxDig);
                    nuevoNumero = int.Parse(nuevoTexto);
                    intento++;
                    if (intento > 1000)
                    {
                        nuevoNumero = 0;
                        break;
                    }
                } while (operador == "-" && acumulado - nuevoNumero < 0);

                if (operador == "+")
                    acumulado += nuevoNumero;
                else
                    acumulado -= nuevoNumero;

                numeros.Add(operador == "-" ? -nuevoNumero : nuevoNumero);
                secuencia.Add($"{operador} {nuevoNumero}");
            }

            var modelo = new RFlashModel
            {
                OperacionTexto = string.Join(" ", secuencia),
                RespuestaCorrecta = acumulado,
                RespuestaUsuario = 0,
                Respondido = false
            };

            HttpContext.Session.SetString("SecuenciaNumeros", JsonSerializer.Serialize(numeros));
            HttpContext.Session.SetString("ResultadoFlash", JsonSerializer.Serialize(modelo));

            // Pasar datos a vista para temporizador
            ViewBag.TiempoMeditacion = config.TiempoMeditacion;
            ViewBag.VelocidadPreguntas = config.VelocidadPreguntas;

            HttpContext.Session.SetString("ConfFlash", JsonSerializer.Serialize(config));


            return View();
        }


        [HttpGet]
        public IActionResult EjercicioFlash()
        {
            var numerosJson = HttpContext.Session.GetString("SecuenciaNumeros");
            var numeros = string.IsNullOrEmpty(numerosJson)
                ? new List<int>()
                : JsonSerializer.Deserialize<List<int>>(numerosJson);

            var velocidad = TempData["VelocidadPreguntas"]?.ToString() ?? "1.0";
            var total = numeros.Count;

            ViewBag.Velocidad = float.Parse(velocidad, System.Globalization.CultureInfo.InvariantCulture);
            ViewBag.TotalEjercicios = total;
            ViewBag.Secuencia = JsonSerializer.Serialize(numeros);

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
        public IActionResult ResultadoFlash(string respuestaUsuario)
        {
            // Leer el modelo desde Session
            var resultadoJson = HttpContext.Session.GetString("ResultadoFlash");
            var resultado = string.IsNullOrEmpty(resultadoJson)
                ? new RFlashModel()
                : JsonSerializer.Deserialize<RFlashModel>(resultadoJson);

            // Evaluar respuesta del usuario
            if (string.IsNullOrWhiteSpace(respuestaUsuario))
            {
                resultado.Respondido = false;
                resultado.RespuestaUsuario = -1;
            }
            else
            {
                resultado.Respondido = true;
                resultado.RespuestaUsuario = int.TryParse(respuestaUsuario, out var valor) ? valor : -1;
            }

            // Evaluar si fue correcta
            bool fueCorrecta = resultado.Respondido && resultado.RespuestaUsuario == resultado.RespuestaCorrecta;

            // Obtener historial desde Session
            var historialStr = HttpContext.Session.GetString("HistorialFlash");
            var sesiones = string.IsNullOrEmpty(historialStr)
                ? new List<SesionFlashModel>()
                : JsonSerializer.Deserialize<List<SesionFlashModel>>(historialStr);

            // Agregar esta sesión
            int cantidad = resultado?.OperacionTexto?.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length / 2 + 1 ?? 1;

            sesiones.Add(new SesionFlashModel
            {
                TotalEjercicios = cantidad,
                FueCorrecta = fueCorrecta
            });


            // Guardar de nuevo en Session
            HttpContext.Session.SetString("HistorialFlash", JsonSerializer.Serialize(sesiones));
            HttpContext.Session.SetString("ResultadoFlash", JsonSerializer.Serialize(resultado));

            return View("ResultadoFlash", resultado);
        }


        [HttpPost]
        public IActionResult FinalizarDesdeEjercicio()
        {
            var resultadoJson = HttpContext.Session.GetString("ResultadoFlash");
            var resultado = string.IsNullOrEmpty(resultadoJson)
                ? new RFlashModel()
                : JsonSerializer.Deserialize<RFlashModel>(resultadoJson);

            resultado.Respondido = false;
            resultado.RespuestaUsuario = -1;

            // Guardar como incorrecta
            var historialStr = HttpContext.Session.GetString("HistorialFlash");
            var sesiones = string.IsNullOrEmpty(historialStr)
                ? new List<SesionFlashModel>()
                : JsonSerializer.Deserialize<List<SesionFlashModel>>(historialStr);

            int cantidad = resultado?.OperacionTexto?.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length / 2 + 1 ?? 1;

            sesiones.Add(new SesionFlashModel
            {
                TotalEjercicios = cantidad,
                FueCorrecta = false
            });

            HttpContext.Session.SetString("ResultadoFlash", JsonSerializer.Serialize(resultado));
            HttpContext.Session.SetString("HistorialFlash", JsonSerializer.Serialize(sesiones));

            return View("ResultadoFlash", resultado);
        }


    }
}
