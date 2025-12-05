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
        public DashboardController(IUsersDBService usersDBService, IClasesDBService clasesDBService)
        {
            this.usersDBService = usersDBService;
            this.clasesDBService = clasesDBService;
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

        public IActionResult Memorizacion()
        {
            var userInfo = GetUserInfo();
            if (userInfo.Id_Usuario == 0)
            {
                return RedirectToAction("Inicio", "Inicio");
            }

            return View(userInfo);
        }

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
                    Frase_Bienvenida = FraseBienvenida
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