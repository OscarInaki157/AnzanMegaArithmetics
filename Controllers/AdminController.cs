using AnzanMegaArithmetics.Models;
using AnzanMegaArithmetics.Services;
using DataBase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AnzanMegaArithmetics.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        //instanciar clase de users service
        private readonly IUsersDBService _usersDBService;
        private readonly IClasesDBService _clasesDBService;
        private readonly IPruebasDBService _pruebasDBService;

        public AdminController(IUsersDBService usersDBService, IClasesDBService clasesDBService, IPruebasDBService pruebasDBService)
        {
            this._usersDBService = usersDBService;
            this._clasesDBService = clasesDBService;
            this._pruebasDBService = pruebasDBService;
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
                Licencia = User.FindFirst("Licencia")?.Value
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

     


    }
}
