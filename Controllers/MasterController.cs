using AnzanMegaArithmetics.Models;
using AnzanMegaArithmetics.Models.MasterModels;
using AnzanMegaArithmetics.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AnzanMegaArithmetics.Controllers
{
    [Authorize(Roles = "Master")]
    public class MasterController : Controller
    {
        private readonly IMasterDBService _masterDBService;
        private readonly IUsersDBService _usersDBService;

        public MasterController(IMasterDBService masterDBService, IUsersDBService usersDBService)
        {
            this._masterDBService = masterDBService;
            this._usersDBService = usersDBService;
        }

        [HttpGet]
        public async Task<IActionResult> PanelMaster()
        {
            var userInfo = GetUserInfo();
            if (userInfo.Id_Usuario == 0) return RedirectToAction("Inicio", "Inicio");

            var modelo = await _masterDBService.ObtenerDatosDashboardAsync();

            modelo.Id_Usuario = userInfo.Id_Usuario;
            modelo.Id_Rol = userInfo.Id_Rol;
           
            modelo.Exp = userInfo.Exp;

            return View(modelo);
        }

        [HttpGet]
        public IActionResult ObtenerFormularioInstitucion()
        {
            var model = new CrearInstitucionModel { LicenciasIniciales = 1 };
            return PartialView("~/Views/Shared/Partials/Panels/Master/_CrearInstitucion.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> CrearInstitucion(CrearInstitucionModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { exito = false, mensaje = "Datos inválidos. Revisa el formulario." });
            }

            var (exito, mensaje) = await _masterDBService.CrearInstitucionAsync(model);
            return Json(new { exito, mensaje });
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerGestionLicencias(int id)
        {
            var model = await _masterDBService.ObtenerDatosLicenciasAsync(id);
            return PartialView("~/Views/Shared/Partials/Panels/Master/_GestionLicencias.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> ActualizarLicencias(GestionLicenciasModel model)
        {
            var (exito, mensaje) = await _masterDBService.ActualizarLicenciasAsync(model);
            return Json(new { exito, mensaje });
        }

        [HttpGet]
        public async Task<IActionResult> EditarInstitucion(int id)
        {
            var model = await _masterDBService.ObtenerInstitucionParaEdicionAsync(id);

            if (model == null) return NotFound();

            return PartialView("~/Views/Shared/Partials/Panels/Master/_EditarInstitucion.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> GuardarEdicionInstitucion(EditarInstitucionModel model)
        {
            var resultado = await _masterDBService.EditarNombreInstitucionAsync(model.Id_Institucion, model.NuevoNombre);

            return Json(new { exito = resultado.Exito, mensaje = resultado.Mensaje });
        }

        [HttpGet]
        public async Task<IActionResult> DetalleInstitucion(int id)
        {
            var userInfo = GetUserInfo();
            if (userInfo.Id_Usuario == 0) return RedirectToAction("Inicio", "Inicio");

            var modelo = await _masterDBService.ObtenerDetalleInstitucionAsync(id);

            if (modelo == null) return RedirectToAction("Inicio", "Inicio");

            modelo.Id_Usuario = userInfo.Id_Usuario;
            modelo.Id_Rol = userInfo.Id_Rol;
            modelo.Exp = userInfo.Exp;
          

            return View(modelo);
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

                LoginResponseModel response = _usersDBService.ObtenerUserDashboard(idUsuario);

                if (response == null || response.Id_Usuario == 0)
                {
                    return new LoginResponseModel { Id_Usuario = 0 };
                }


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
                    Rango_Actual = response.Rango_Actual
                };
            }
            catch (Exception)
            {
                return new LoginResponseModel { Id_Usuario = 0 };
            }
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerFormularioNuevaClase(int idInstitucion)
        {
            var model = new CrearClaseModel { Id_Institucion = idInstitucion };
            return PartialView("~/Views/Shared/Partials/Panels/Master/_CrearClase.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> CrearClase(CrearClaseModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { exito = false, mensaje = "Datos inválidos." });

            var (exito, mensaje) = await _masterDBService.CrearClaseAsync(model);
            return Json(new { exito, mensaje });
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerFormularioEditarClase(int idClase)
        {
            var model = await _masterDBService.ObtenerClaseParaEdicionAsync(idClase);
            if (model == null) return NotFound();
            return PartialView("~/Views/Shared/Partials/Panels/Master/_EditarClase.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> GuardarEdicionClase(EditarClaseModel model)
        {
            var (exito, mensaje) = await _masterDBService.EditarNombreClaseAsync(model);
            return Json(new { exito, mensaje });
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerTabAlumnos(int idInstitucion, string clase = "Todas")
        {
            var model = await _masterDBService.ObtenerAlumnosInstitucionAsync(idInstitucion, clase);
            return PartialView("~/Views/Shared/Partials/Panels/Master/_ListadoAlumnosMaster.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerFichaAlumnoMaster(int idUsuario)
        {
            var model = await _masterDBService.ObtenerFichaAlumnoAsync(idUsuario);
            if (model == null) return NotFound();
            return PartialView("~/Views/Shared/Partials/Panels/Master/_FichaAlumnoMaster.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerFormularioCrearAlumno(int idInstitucion)
        {
            var model = await _masterDBService.ObtenerFormularioCrearAlumnoAsync(idInstitucion);
            return PartialView("~/Views/Shared/Partials/Panels/Master/_CrearAlumnoMaster.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> CrearAlumnoMaster(CrearAlumnoMasterModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { exito = false, mensaje = "Datos inválidos. Revisa el formulario." });

            var (exito, mensaje) = await _masterDBService.CrearAlumnoMasterAsync(model);
            return Json(new { exito, mensaje });
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerFormularioEditarAlumnoMaster(int idUsuario)
        {
            var model = await _masterDBService.ObtenerDatosEditarAlumnoMasterAsync(idUsuario);
            if (model == null) return NotFound();
            return PartialView("~/Views/Shared/Partials/Panels/Master/_EditarAlumnoMaster.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> GuardarEdicionAlumnoMaster(EditarAlumnoMasterModel model)
        {
            var (exito, mensaje) = await _masterDBService.GuardarEdicionAlumnoMasterAsync(model);
            return Json(new { exito, mensaje });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleActivoAlumno(int idUsuario)
        {
            var (exito, mensaje) = await _masterDBService.ToggleActivoAlumnoAsync(idUsuario);
            return Json(new { exito, mensaje });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleActivoClase(int idClase)
        {
            var (exito, mensaje) = await _masterDBService.ToggleActivoClaseAsync(idClase);
            return Json(new { exito, mensaje });
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerTabProfesores(int idInstitucion)
        {
            var model = await _masterDBService.ObtenerProfesoresInstitucionAsync(idInstitucion);
            return PartialView("~/Views/Shared/Partials/Panels/Master/_ListadoProfesMaster.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerFichaProfesorMaster(int idUsuario)
        {
            var model = await _masterDBService.ObtenerFichaAlumnoAsync(idUsuario); // mismo método, mismo modelo
            if (model == null) return NotFound();
            return PartialView("~/Views/Shared/Partials/Panels/Master/_FichaAlumnoMaster.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerFormularioCrearProfesor(int idInstitucion)
        {
            var model = await _masterDBService.ObtenerFormularioCrearProfesorAsync(idInstitucion);
            return PartialView("~/Views/Shared/Partials/Panels/Master/_CrearProfesorMaster.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> CrearProfesorMaster(CrearProfesorMasterModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { exito = false, mensaje = "Datos inválidos." });
            var (exito, mensaje) = await _masterDBService.CrearProfesorMasterAsync(model);
            return Json(new { exito, mensaje });
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerFormularioEditarProfesorMaster(int idUsuario)
        {
            var model = await _masterDBService.ObtenerDatosEditarProfesorMasterAsync(idUsuario);
            if (model == null) return NotFound();
            return PartialView("~/Views/Shared/Partials/Panels/Master/_EditarProfesorMaster.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> GuardarEdicionProfesorMaster(EditarProfesorMasterModel model)
        {
            var (exito, mensaje) = await _masterDBService.GuardarEdicionProfesorMasterAsync(model);
            return Json(new { exito, mensaje });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleActivoProfesor(int idUsuario)
        {
            var (exito, mensaje) = await _masterDBService.ToggleActivoProfesorAsync(idUsuario);
            return Json(new { exito, mensaje });
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerTabAdmins(int idInstitucion)
        {
            var model = await _masterDBService.ObtenerAdminsInstitucionAsync(idInstitucion);
            return PartialView("~/Views/Shared/Partials/Panels/Master/_ListadoAdminsMaster.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerFormularioCrearAdmin(int idInstitucion)
        {
            var model = await _masterDBService.ObtenerFormularioCrearAdminAsync(idInstitucion);
            return PartialView("~/Views/Shared/Partials/Panels/Master/_CrearAdminMaster.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> CrearAdminMaster(CrearAdminMasterModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { exito = false, mensaje = "Datos inválidos." });
            var (exito, mensaje) = await _masterDBService.CrearAdminMasterAsync(model);
            return Json(new { exito, mensaje });
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerFormularioEditarAdminMaster(int idUsuario)
        {
            var model = await _masterDBService.ObtenerDatosEditarAdminMasterAsync(idUsuario);
            if (model == null) return NotFound();
            return PartialView("~/Views/Shared/Partials/Panels/Master/_EditarAdminMaster.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> GuardarEdicionAdminMaster(EditarAdminMasterModel model)
        {
            var (exito, mensaje) = await _masterDBService.GuardarEdicionAdminMasterAsync(model);
            return Json(new { exito, mensaje });
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerFichaAdminMaster(int idUsuario)
        {
            var model = await _masterDBService.ObtenerFichaAlumnoAsync(idUsuario);
            if (model == null) return NotFound();
            return PartialView("~/Views/Shared/Partials/Panels/Master/_FichaAlumnoMaster.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleActivoAdmin(int idUsuario)
        {
            var (exito, mensaje) = await _masterDBService.ToggleActivoAdminAsync(idUsuario);
            return Json(new { exito, mensaje });
        }

        [HttpPost]
        public async Task<IActionResult> EliminarUsuario(int idUsuario)
        {
            var (exito, mensaje) = await _masterDBService.EliminarUsuarioAsync(idUsuario);
            return Json(new { exito, mensaje });
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerFormularioEliminarClase(int idClase)
        {
            var model = await _masterDBService.ObtenerFormularioEliminarClaseAsync(idClase);
            if (model == null) return NotFound();
            return PartialView("~/Views/Shared/Partials/Panels/Master/_EliminarClase.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> EliminarClase(EliminarClaseModel model)
        {
            var (exito, mensaje) = await _masterDBService.EliminarClaseAsync(model);
            return Json(new { exito, mensaje });
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerFormularioMoverUsuario(int idUsuario)
        {
            var model = await _masterDBService.ObtenerFormularioMoverUsuarioAsync(idUsuario);
            if (model == null) return NotFound();
            return PartialView("~/Views/Shared/Partials/Panels/Master/_MoverUsuario.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerClasesPorInstitucion(int idInstitucion)
        {
            var clases = await _masterDBService.ObtenerClasesPorInstitucionAsync(idInstitucion);
            return Json(clases);
        }

        [HttpPost]
        public async Task<IActionResult> MoverUsuario(MoverUsuarioModel model)
        {
            var (exito, mensaje) = await _masterDBService.MoverUsuarioAsync(model);
            return Json(new { exito, mensaje });
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerFormularioMoverClase(int idClase)
        {
            var model = await _masterDBService.ObtenerFormularioMoverClaseAsync(idClase);
            if (model == null) return NotFound();
            return PartialView("~/Views/Shared/Partials/Panels/Master/_MoverClase.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> MoverClase(MoverClaseModel model)
        {
            var (exito, mensaje) = await _masterDBService.MoverClaseAsync(model);
            return Json(new { exito, mensaje });
        }

    }
}
