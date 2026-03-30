using AnzanMegaArithmetics.Models;
using AnzanMegaArithmetics.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AnzanMegaArithmetics.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IUsersDBService usersDBService;
        private readonly IClasesDBService clasesDBService;
        private readonly IPruebasDBService pruebasDBService;
        public DashboardController(IUsersDBService usersDBService, IClasesDBService clasesDBService, IPruebasDBService pruebasDBService)
        {
            this.usersDBService = usersDBService;
            this.clasesDBService = clasesDBService;
            this.pruebasDBService = pruebasDBService;
        }

        public IActionResult MiPerfil() 
        {
            var userInfo = GetUserInfo();
            if (userInfo.Id_Usuario == 0)
            {
                return RedirectToAction("Inicio", "Inicio");
            }

            List<DailyChallengeViewModel> dailyChallenges = usersDBService.GetUserDailyChallenges(userInfo.Id_Usuario);

            ViewBag.DailyChallenges = dailyChallenges;

            return View(userInfo);
        }


        [HttpPost]
        public IActionResult ClaimReward([FromForm] int retoId)
        {
            var userInfo = GetUserInfo();

            if (userInfo == null || userInfo.Id_Usuario == 0)
            {
                TempData["ErrorMessage"] = "Usuario no autenticado. Por favor, inicia sesión de nuevo.";
                return RedirectToAction("Index", "Home");
            }

            int xpGanada = usersDBService.ClaimChallengeReward(userInfo.Id_Usuario, retoId);

            if (xpGanada > 0)
            {
                TempData["SuccessMessage"] = $"¡Felicidades, {userInfo.Nombre}! Has reclamado la recompensa y ganado {xpGanada} XP.";
            }
            else
            {
                TempData["ErrorMessage"] = "No se pudo reclamar la recompensa. Asegúrate de que el reto esté completado y no haya sido reclamado previamente.";
            }

            return RedirectToAction("MiPerfil", "Dashboard");
        }

        public IActionResult Ranking()
        {
            var userInfo = GetUserInfo();
            if (userInfo.Id_Usuario == 0) return RedirectToAction("Inicio", "Inicio");

            List<RankingSlideModel> rankingSlider = usersDBService.ObtenerRankingsSlider(10);

            ViewBag.Ranking = rankingSlider;

            ViewBag.RankingJson = System.Text.Json.JsonSerializer.Serialize(rankingSlider);

            return View(userInfo);
        }

        [HttpGet]
        public IActionResult ObtenerRankingJson()
        {
            List<RankingSlideModel> rankingCompleto = usersDBService.ObtenerRankingsSlider(50);

            return Json(rankingCompleto, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = null
            });
        }

        [HttpGet]
        public IActionResult ObtenerRankingFiltrado(string periodo, string actividad, int cantidad = 10) 
        {
            if (string.IsNullOrEmpty(periodo) || string.IsNullOrEmpty(actividad))
            {
                return BadRequest(new { Datos = new List<RankingUsersModel>(), Titulo = "Error en la solicitud" });
            }

            RankingSlideModel slideFiltrado = usersDBService.ObtenerRankingFiltrado(periodo, actividad, cantidad);

            return Json(slideFiltrado, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = null
            });

        }

        public IActionResult Dashboard()
        {
            var userInfo = GetUserInfo();
            if (userInfo.Id_Usuario == 0)
            {
                return RedirectToAction("Inicio", "Inicio");
            }

            return View(userInfo);
        }

        public IActionResult Desafios()
        {
            var userInfo = GetUserInfo();
            if (userInfo.Id_Usuario == 0)
            {
                return RedirectToAction("Inicio", "Inicio");
            }

            return View(userInfo);
        }

        public IActionResult Conferencias()
        {
            var userInfo = GetUserInfo();
            if (userInfo.Id_Usuario == 0)
            {
                return RedirectToAction("Inicio", "Inicio");
            }

            return View(userInfo);
        }

        public IActionResult HojasEjercicios()
        {
            var userInfo = GetUserInfo();
            if (userInfo.Id_Usuario == 0)
            {
                return RedirectToAction("Inicio", "Inicio");
            }

            return View(userInfo);
        }

        public IActionResult Memorizacion()
        {
            var userInfo = GetUserInfo();
            if (userInfo.Id_Usuario == 0)
            {
                return RedirectToAction("Inicio", "Inicio");
            }

            return View(userInfo);
        }

        //panel profesor
        [HttpGet]
        public IActionResult PanelProfesor()
        {
            var userInfo = GetUserInfo();
            if (userInfo.Id_Usuario == 0) return RedirectToAction("Inicio", "Inicio");

            string claseInicial = userInfo.Clases?.FirstOrDefault() ?? string.Empty;

            ResumenClaseViewModel model = !string.IsNullOrEmpty(claseInicial)
                ? pruebasDBService.ObtenerResumenPorClase(claseInicial)
                : new ResumenClaseViewModel();

            model.Id_Usuario = userInfo.Id_Usuario;
            model.Id_Rol = userInfo.Id_Rol;
            model.Nombre = userInfo.Nombre;
            model.Gamer_Tag = userInfo.Gamer_Tag;
            model.Licencia = userInfo.Licencia;
            model.Exp = userInfo.Exp;
            model.Ultima_Cnx = userInfo.Ultima_Cnx;
            model.Clases = userInfo.Clases ?? new List<string>();

            model.ClasesDisponibles = model.Clases;
            model.ClaseActual = model.ClasesDisponibles.FirstOrDefault();
            model.ListaAlumnos = usersDBService.ObtenerAlumnosPorClase(model.ClaseActual);
            model.TiposDePruebaDisponibles = pruebasDBService.ObtenerTiposDePruebaDisponibles();

            return View(model);
        }

        [HttpGet]
        public JsonResult ObtenerDatosClase(string idClase)
        {
            try
            {
                if (string.IsNullOrEmpty(idClase))
                {
                    return Json(new { exito = false, mensaje = "Clase no válida" });
                }

                var resumen = pruebasDBService.ObtenerResumenPorClase(idClase);

                return Json(new { exito = true, datos = resumen });
            }
            catch (Exception ex)
            {
                return Json(new { exito = false, mensaje = "Error al cargar los datos de la clase." });
            }
        }

        [HttpGet]
        public IActionResult FiltrarAlumnosPorClase(string clase)
        {
            try
            {
                var userInfo = GetUserInfo();

                var model = new ResumenClaseViewModel();

                model.ClaseActual = clase;
                model.ListaAlumnos = usersDBService.ObtenerAlumnosPorClase(clase);

                model.ClasesDisponibles = userInfo.Clases ?? new List<string>();

                return PartialView("~/Views/Shared/Partials/Panels/_ListadoAlumnos.cshtml", model);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet]
        public IActionResult ObtenerDetallesAlumno(int idUsuario)
        {
            try
            {
                var alumno = usersDBService.ObtenerAlumnoPorId(idUsuario);

                if (alumno == null)
                {
                    return NotFound("<div class='alert alert-danger'>Alumno no encontrado en la base de datos.</div>");
                }

                return PartialView("~/Views/Shared/Partials/Modals/_DetallesAlumno.cshtml", alumno);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"<div class='alert alert-danger'>Error del servidor: {ex.Message}</div>");
            }
        }

        [HttpGet]
        public IActionResult EditarAlumno(int idUsuario)
        {
            var alumno = usersDBService.ObtenerAlumnoPorId(idUsuario);
            if (alumno == null) return NotFound();

            return PartialView("~/Views/Shared/Partials/Modals/_EditarAlumno.cshtml", alumno);
        }

        [HttpPost]
        public IActionResult GuardarCambiosAlumno(ActualizarUsuarioModel model)
        {
            try
            {
                string resultado = usersDBService.ActualizarDatosBasicosJugador(model);

                if (resultado.Contains("correctamente"))
                {
                    return Json(new { exito = true, mensaje = resultado });
                }
                return Json(new { exito = false, mensaje = resultado });
            }
            catch (Exception ex)
            {
                return Json(new { exito = false, mensaje = "Error crítico: " + ex.Message });
            }
        }
        [HttpGet]
        public IActionResult FiltrarHistorialPruebas(string Clase, string Alumno, string TipoPrueba, DateTime? FechaInicio, DateTime? FechaFin)
        {
            try
            {
                var resultados = pruebasDBService.ObtenerHistorialFiltrado(Clase, Alumno, TipoPrueba, FechaInicio, FechaFin);

                return PartialView("~/Views/Shared/Partials/Panels/_ResultadosPruebas.cshtml", resultados);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet]
        public IActionResult ObtenerConfiguracionPrueba(int idPrueba)
        {
            try
            {
                var config = pruebasDBService.ObtenerConfiguracionPorId(idPrueba);

                if (config == null)
                    return NotFound("<div class='alert alert-danger'>Prueba no encontrada.</div>");

                return PartialView("~/Views/Shared/Partials/Modals/_ConfiguracionPrueba.cshtml", config);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
        //panel profesor

        public IActionResult PanelAdministrador()
        {
            int usersCount = usersDBService.ListarUsersTotales();
            int clasesCount = clasesDBService.ListarClasesTotales();

            var userInfo = GetUserInfo();
            if (userInfo.Id_Usuario == 0)
            {
                return RedirectToAction("Inicio", "Inicio");
            }

            ViewBag.UsersCount = usersCount;
            ViewBag.ClasesCount = clasesCount;

            return View(userInfo);
        }

        private LoginResponseModel GetUserInfo()
        {
            try
            {
                int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int idUsuario);

                if (idUsuario == 0)
                {
                    return new LoginResponseModel { Id_Usuario = 0 };
                }

                LoginResponseModel response = usersDBService.ObtenerUserDashboard(idUsuario);

                if (response == null || response.Id_Usuario == 0)
                {
                    return new LoginResponseModel { Id_Usuario = 0 };
                }

                string FraseBienvenida = SetFraseBienvenida(response.Nombre);

                HttpContext.Session.SetInt32("Id_Usuario", response.Id_Usuario);

                return new LoginResponseModel
                {
                    Id_Usuario = response.Id_Usuario,
                    Nombre = response.Nombre,
                    Id_Rol = response.Id_Rol,
                    Gamer_Tag = response.Gamer_Tag,
                    Correo = response.Correo,
                    Clases = response.Clases,
                    Racha = response.Racha,
                    Exp = response.Exp,
                    Ultima_Cnx = response.Ultima_Cnx,
                    Licencia = response.Licencia,
                    Frase_Bienvenida = FraseBienvenida,
                    Rango_Actual = response.Rango_Actual
                };
            }
            catch (Exception)
            {
                return new LoginResponseModel { Id_Usuario = 0 };
            }
        }

        private string SetFraseBienvenida(string nombreCompleto)
        {
            string primerNombre;
            int indiceEspacio = nombreCompleto.IndexOf(' ');

            if (indiceEspacio > 0)
            {
                primerNombre = nombreCompleto.Substring(0, indiceEspacio);
            }
            else
            {
                primerNombre = nombreCompleto;
            }

            if (string.IsNullOrEmpty(primerNombre))
            {
                primerNombre = "Usuario";
            }

            var frases = new List<string>
            {
                $"¡Hola {primerNombre}, bienvenido a Mentes México!",
                $"¡{primerNombre}, hoy es un gran día para aprender!",
                $"¡Tu mente es poderosa, {primerNombre}!",
                $"¡Listo para un nuevo desafío, {primerNombre}!",
                $"¡Vamos a hacer magia con los números, {primerNombre}!",
                $"¡Cada clic te acerca a la maestría, {primerNombre}!"
            };

            var random = new Random();
            int index = random.Next(frases.Count);
            string FraseBienvenida = frases[index];

            return FraseBienvenida;
        }
    }
}