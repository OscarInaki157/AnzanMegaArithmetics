using AnzanMegaArithmetics.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AnzanMegaArithmetics.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        public DashboardController()
        {
        }

        public IActionResult MiPerfil() 
        {
            var userInfo = GetUserInfo();
            if (userInfo.Id_Usuario == 0)
            {
                return RedirectToAction("Inicio", "Inicio");
            }

            SetViewBag(userInfo);
            SetFraseBienvenida(userInfo.Nombre);

            return View();
        }

        public IActionResult Dashboard()
        {
            var userInfo = GetUserInfo();
            if (userInfo.Id_Usuario == 0)
            {
                return RedirectToAction("Inicio", "Inicio");
            }

            SetViewBag(userInfo);
            SetFraseBienvenida(userInfo.Nombre);

            return View();
        }

        public IActionResult Desafios()
        {
            var userInfo = GetUserInfo();
            if (userInfo.Id_Usuario == 0)
            {
                return RedirectToAction("Inicio", "Inicio");
            }

            SetViewBag(userInfo);
            SetFraseBienvenida(userInfo.Nombre);

            return View();
        }

        public IActionResult Conferencias()
        {
            var userInfo = GetUserInfo();
            if (userInfo.Id_Usuario == 0)
            {
                return RedirectToAction("Inicio", "Inicio");
            }

            SetViewBag(userInfo);
            SetFraseBienvenida(userInfo.Nombre);

            return View();
        }

        public IActionResult Memorizacion()
        {
            var userInfo = GetUserInfo();
            if (userInfo.Id_Usuario == 0)
            {
                return RedirectToAction("Inicio", "Inicio");
            }

            SetViewBag(userInfo);
            SetFraseBienvenida(userInfo.Nombre);

            return View();
        }

        private LoginResponseModel GetUserInfo()
        {
            var clasesClaim = User.FindFirst("Clases")?.Value;
            var listaClases = !string.IsNullOrEmpty(clasesClaim)
                ? clasesClaim.Split(',').ToList()
                : new List<string>();

            int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int idUsuario);
            int.TryParse(User.FindFirst("Racha")?.Value, out int racha);
            int.TryParse(User.FindFirst("Exp")?.Value, out int exp);
            DateTime.TryParse(User.FindFirst("UltimaCnx")?.Value, out DateTime ultimaCnx);

            return new LoginResponseModel
            {
                Id_Usuario = idUsuario,
                Nombre = User.FindFirst(ClaimTypes.Name)?.Value,
                Id_Rol = User.FindFirst(ClaimTypes.Role)?.Value,
                Gamer_Tag = User.FindFirst("Usuario")?.Value,
                Correo = User.FindFirst("Correo")?.Value,
                Clases = listaClases,
                Racha = racha,
                Exp = exp,
                Ultima_Cnx = ultimaCnx,
                Licencia= User.FindFirst("Licencia")?.Value
            };
        }

        private void SetViewBag(LoginResponseModel userInfo)
        {
            ViewBag.IdUsuario = userInfo.Id_Usuario;
            ViewBag.Nombre = userInfo.Nombre;
            ViewBag.Rol = userInfo.Id_Rol;
            ViewBag.Gamer_Tag = userInfo.Gamer_Tag;
            ViewBag.Correo = userInfo.Correo;
            ViewBag.Clases = userInfo.Clases;
            ViewBag.PrimeraClase = userInfo.Clases.FirstOrDefault() ?? "Sin clase asignada";

            ViewBag.Racha = userInfo.Racha;
            ViewBag.Exp = userInfo.Exp;
            ViewBag.UltimaCnx = userInfo.Ultima_Cnx.ToString("dd/MM/yyyy HH:mm");

            ViewBag.Licencia = userInfo.Licencia;
        }

        private void SetFraseBienvenida(string nombre)
        {
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
            ViewBag.FraseBienvenida = frases[index];
        }
    }
}