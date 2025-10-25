using AnzanMegaArithmetics.Models;
using AnzanMegaArithmetics.Services;
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

        public AdminController(IUsersDBService usersDBService, IClasesDBService clasesDBService)
        {
            this._usersDBService = usersDBService;
            this._clasesDBService = clasesDBService;
        }

        public IActionResult AdminUsers()
        {
            var userInfo = GetUserInfo();
            if (userInfo.Id_Usuario == 0)
            {
                return RedirectToAction("Inicio", "Inicio");
            }

            //recuperar todos los usuarios
            ListUsersModel usuarios;
            try 
            {
                usuarios = _usersDBService.ObtenerUsuarios();
            } catch (Exception ex) 
            {
                return RedirectToAction("Inicio", "Inicio");
            }

            SetViewBag(userInfo);
            return View(usuarios);
        }

        public IActionResult AdminClases()
        {
            var userInfo = GetUserInfo();
            if (userInfo.Id_Usuario == 0)
            {
                return RedirectToAction("Inicio", "Inicio");
            }

            //recuperar todas las clases
            List<ClaseBDModel> clases = new List<ClaseBDModel>();
            try 
            {
                clases = _clasesDBService.ObtenerClases();
            } catch (Exception ex) 
            {
                return RedirectToAction("Inicio", "Inicio");
            }


            SetViewBag(userInfo);
            return View(clases);
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

        //crud de users
        [HttpPost]
        public ActionResult ActualizarUsuario(ActualizarUsuarioModel user)
        {
            if (user == null || user.Id_Usuario == 0)
            {
                TempData["ErrorMessage"] = "Datos de usuario inválidos.";
                return RedirectToAction("AdminUsers");
            }

            string mensaje = string.Empty;
            try
            {
                mensaje = _usersDBService.ActualizarUser(user);
                if (mensaje.Contains("Error") || mensaje.StartsWith("Error")) 
                {
                    TempData["ErrorMessage"] = mensaje;
                    return RedirectToAction("AdminUsers");
                }

                TempData["SuccessMessage"] = mensaje;
                return RedirectToAction("AdminUsers");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al actualizar el usuario: " + ex.Message;
                return RedirectToAction("AdminUsers");
            }
            
        }

        [HttpPost]
        public ActionResult CrearUsuario(ActualizarUsuarioModel model)
        {
            if (model == null)
            {
                TempData["ErrorMessage"] = "Datos de usuario inválidos.";
                return RedirectToAction("AdminUsers");
            }

            string mensaje = string.Empty;
            try
            {
                mensaje = _usersDBService.CrearNuevoUsuario(model);
                if (mensaje.Contains("Error") || mensaje.StartsWith("Error"))
                {
                    TempData["ErrorMessage"] = mensaje;
                    return RedirectToAction("AdminUsers");
                }

                TempData["SuccessMessage"] = mensaje;
                return RedirectToAction("AdminUsers");
            }
            catch (Exception ex) 
            {
                TempData["ErrorMessage"] = "Error al agregar el usuario: " + ex.Message;
                return RedirectToAction("AdminUsers");
            }
        }

        [HttpPost]
        public ActionResult EliminarUsuario(ActualizarUsuarioModel model) 
        {
            if (model == null || model.Id_Usuario == 0)
            {
                TempData["ErrorMessage"] = "Datos de usuario inválidos.";
                return RedirectToAction("AdminUsers");
            }

            string mensaje = string.Empty;
            try
            {
                mensaje = _usersDBService.EliminarUsuario(model);
                if (mensaje.Contains("Error") || mensaje.StartsWith("Error"))
                {
                    TempData["ErrorMessage"] = mensaje;
                    return RedirectToAction("AdminUsers");
                }

                TempData["SuccessMessage"] = mensaje;
                return RedirectToAction("AdminUsers");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al eliminar el usuario: " + ex.Message;
                return RedirectToAction("AdminUsers");
            }
        }

        //crud de clases
        [HttpPost]
        public ActionResult ActualizarClase(ActualizarClaseModel model)
        {
            if (model == null || model.Id_Clase == 0)
            {
                TempData["ErrorMessage"] = "Datos de clase inválidos.";
                return RedirectToAction("AdminClases");
            }

            string mensaje = string.Empty;

            try
            {
                mensaje = _clasesDBService.ActualizarClase(model);
                if (mensaje.Contains("Error") || mensaje.StartsWith("Error"))
                {
                    TempData["ErrorMessage"] = mensaje;
                    return RedirectToAction("AdminClases");
                }
                TempData["SuccessMessage"] = mensaje;
                return RedirectToAction("AdminClases");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al actualizar la clase: " + ex.Message;
                return RedirectToAction("AdminClases");
            }
        }

    }
}
