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

            return View(userInfo);
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

        private string SetFraseBienvenida(string nombre)
        {
            string FraseBienvenida = string.Empty;
            var frases = new List<string>
            {
                $"¡Hola {nombre}, bienvenido a Mentes México!",
                $"¡{nombre}, hoy es un gran día para aprender!",
                $"¡Tu mente es poderosa, {nombre}!",
                $"¡Listo para un nuevo desafío, {nombre}!",
                $"¡Vamos a hacer magia con los números, {nombre}!",
                $"¡Cada clic te acerca a la maestría, {nombre}!"
            };

            var random = new Random();
            int index = random.Next(frases.Count);
            FraseBienvenida = frases[index];

            return FraseBienvenida;
        }
    }
}