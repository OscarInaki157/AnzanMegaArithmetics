using AnzanMegaArithmetics.Models;
using AnzanMegaArithmetics.Models.MasterModels;
using AnzanMegaArithmetics.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AnzanMegaArithmetics.Controllers
{
    // Master: acceso total. Administrador: solo su institución y sin crear licencias.
    [Authorize(Roles = ROL_MASTER + "," + ROL_ADMIN)]
    public class MasterController : Controller
    {
        // ⚠️ Deben coincidir EXACTAMENTE con los nombres de la tabla Roles
        public const string ROL_MASTER = "Master";
        public const string ROL_ADMIN = "Administrador";
        private const int ID_ROL_MASTER = 4;
        private readonly IMasterDBService _masterDBService;
        private readonly IUsersDBService _usersDBService;

        public MasterController(IMasterDBService masterDBService, IUsersDBService usersDBService)
        {
            this._masterDBService = masterDBService;
            this._usersDBService = usersDBService;
        }

        // ===================== PERMISOS =====================

        private bool EsMaster => User.IsInRole(ROL_MASTER);

        private int? _institucionAdminCache;
        private bool _institucionAdminCargada;

        /// <summary>Institución del usuario logueado (solo relevante para Administradores).</summary>
        private async Task<int?> InstitucionDelAdminAsync()
        {
            if (_institucionAdminCargada) return _institucionAdminCache;
            int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int idUsuario);
            _institucionAdminCache = idUsuario > 0
                ? await _masterDBService.ObtenerInstitucionDeUsuarioAsync(idUsuario)
                : null;
            _institucionAdminCargada = true;
            return _institucionAdminCache;
        }

        private async Task<bool> PuedeInstitucionAsync(int? idInstitucion)
        {
            if (EsMaster) return true;
            var propia = await InstitucionDelAdminAsync();
            return propia.HasValue && idInstitucion.HasValue && propia.Value == idInstitucion.Value;
        }

        private async Task<bool> PuedeUsuarioAsync(int idUsuario)
        {
            if (EsMaster) return true;
            // Un Administrador nunca puede tocar a un Master
            var rol = await _masterDBService.ObtenerRolDeUsuarioAsync(idUsuario);
            if (rol == null || rol == ID_ROL_MASTER) return false;
            return await PuedeInstitucionAsync(await _masterDBService.ObtenerInstitucionDeUsuarioAsync(idUsuario));
        }

        private async Task<bool> PuedeClaseAsync(int idClase)
        {
            if (EsMaster || idClase <= 0) return true; // 0 = "sin clase"
            return await PuedeInstitucionAsync(await _masterDBService.ObtenerInstitucionDeClaseAsync(idClase));
        }

        private async Task<bool> PuedeClasesAsync(IEnumerable<int>? ids)
        {
            if (EsMaster || ids == null) return true;
            foreach (var id in ids)
                if (!await PuedeClaseAsync(id)) return false;
            return true;
        }

        private async Task<bool> PuedeLicenciaAsync(int idLicencia)
        {
            if (EsMaster || idLicencia <= 0) return true; // 0 = "sin licencia"
            return await PuedeInstitucionAsync(await _masterDBService.ObtenerInstitucionDeLicenciaAsync(idLicencia));
        }

        private bool RolPermitido(int idRol) => EsMaster || idRol != ID_ROL_MASTER;

        private async Task<List<ClaseSedeModel>?> FiltrarClasesAsync(List<ClaseSedeModel>? clases)
        {
            if (EsMaster || clases == null) return clases;
            var propia = await InstitucionDelAdminAsync();
            return clases.Where(c => c.Id_Institucion == propia).ToList();
        }

        private JsonResult SinPermisoJson() =>
            Json(new { exito = false, mensaje = "No tienes permiso para realizar esta acción." });

        private ContentResult SinPermisoHtml() => new ContentResult
        {
            StatusCode = 403,
            ContentType = "text/html",
            Content = "<div class='alert alert-danger'>No tienes permiso para ver esta información.</div>"
        };

        [HttpGet]
        public async Task<IActionResult> PanelMaster()
        {
            var userInfo = GetUserInfo();
            if (userInfo.Id_Usuario == 0) return RedirectToAction("Inicio", "Inicio");

            if (!EsMaster)
            {
                var propia = await InstitucionDelAdminAsync();
                if (!propia.HasValue) return RedirectToAction("Dashboard", "Dashboard");
                return RedirectToAction(nameof(DetalleInstitucion), new { id = propia.Value });
            }

            var modelo = await _masterDBService.ObtenerDatosDashboardAsync();

            modelo.Id_Usuario = userInfo.Id_Usuario;
            modelo.Id_Rol = userInfo.Id_Rol;
           
            modelo.Exp = userInfo.Exp;

            return View(modelo);
        }

        [HttpGet]
        [Authorize(Roles = ROL_MASTER)]
        public IActionResult ObtenerFormularioInstitucion()
        {
            var model = new CrearInstitucionModel { LicenciasIniciales = 1 };
            return PartialView("~/Views/Shared/Partials/Panels/Master/_CrearInstitucion.cshtml", model);
        }

        [HttpPost]
        [Authorize(Roles = ROL_MASTER)]
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
        [Authorize(Roles = ROL_MASTER)]
        public async Task<IActionResult> ObtenerGestionLicencias(int id)
        {
            var model = await _masterDBService.ObtenerDatosLicenciasAsync(id);
            return PartialView("~/Views/Shared/Partials/Panels/Master/_GestionLicencias.cshtml", model);
        }

        [HttpPost]
        [Authorize(Roles = ROL_MASTER)]
        public async Task<IActionResult> ActualizarLicencias(GestionLicenciasModel model)
        {
            var (exito, mensaje) = await _masterDBService.ActualizarLicenciasAsync(model);
            return Json(new { exito, mensaje });
        }

        [HttpGet]
        public async Task<IActionResult> EditarInstitucion(int id)
        {
            if (!(await PuedeInstitucionAsync(id))) return SinPermisoHtml();
            var model = await _masterDBService.ObtenerInstitucionParaEdicionAsync(id);

            if (model == null) return NotFound();

            return PartialView("~/Views/Shared/Partials/Panels/Master/_EditarInstitucion.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> GuardarEdicionInstitucion(EditarInstitucionModel model)
        {
            if (!(await PuedeInstitucionAsync(model.Id_Institucion))) return SinPermisoJson();
            var resultado = await _masterDBService.EditarNombreInstitucionAsync(model.Id_Institucion, model.NuevoNombre);

            return Json(new { exito = resultado.Exito, mensaje = resultado.Mensaje });
        }

        [HttpGet]
        public async Task<IActionResult> DetalleInstitucion(int id)
        {
            var userInfo = GetUserInfo();
            if (userInfo.Id_Usuario == 0) return RedirectToAction("Inicio", "Inicio");

            if (!await PuedeInstitucionAsync(id)) return RedirectToAction(nameof(PanelMaster));

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
            if (!(await PuedeInstitucionAsync(idInstitucion))) return SinPermisoHtml();
            var model = new CrearClaseModel { Id_Institucion = idInstitucion };
            return PartialView("~/Views/Shared/Partials/Panels/Master/_CrearClase.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> CrearClase(CrearClaseModel model)
        {
            if (!(await PuedeInstitucionAsync(model.Id_Institucion))) return SinPermisoJson();
            if (!ModelState.IsValid)
                return Json(new { exito = false, mensaje = "Datos inválidos." });

            var (exito, mensaje) = await _masterDBService.CrearClaseAsync(model);
            return Json(new { exito, mensaje });
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerFormularioEditarClase(int idClase)
        {
            if (!(await PuedeClaseAsync(idClase))) return SinPermisoHtml();
            var model = await _masterDBService.ObtenerClaseParaEdicionAsync(idClase);
            if (model == null) return NotFound();
            return PartialView("~/Views/Shared/Partials/Panels/Master/_EditarClase.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> GuardarEdicionClase(EditarClaseModel model)
        {
            if (!(await PuedeClaseAsync(model.Id_Clase))) return SinPermisoJson();
            var (exito, mensaje) = await _masterDBService.EditarNombreClaseAsync(model);
            return Json(new { exito, mensaje });
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerTabAlumnos(int idInstitucion, string clase = "Todas")
        {
            if (!(await PuedeInstitucionAsync(idInstitucion))) return SinPermisoHtml();
            var model = await _masterDBService.ObtenerAlumnosInstitucionAsync(idInstitucion, clase);
            return PartialView("~/Views/Shared/Partials/Panels/Master/_ListadoAlumnosMaster.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerFichaAlumnoMaster(int idUsuario)
        {
            if (!(await PuedeUsuarioAsync(idUsuario))) return SinPermisoHtml();
            var model = await _masterDBService.ObtenerFichaAlumnoAsync(idUsuario);
            if (model == null) return NotFound();
            return PartialView("~/Views/Shared/Partials/Panels/Master/_FichaAlumnoMaster.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerFormularioCrearAlumno(int idInstitucion)
        {
            if (!(await PuedeInstitucionAsync(idInstitucion))) return SinPermisoHtml();
            var model = await _masterDBService.ObtenerFormularioCrearAlumnoAsync(idInstitucion);
            model.ClasesDisponibles = await FiltrarClasesAsync(model.ClasesDisponibles);
            return PartialView("~/Views/Shared/Partials/Panels/Master/_CrearAlumnoMaster.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> CrearAlumnoMaster(CrearAlumnoMasterModel model)
        {
            if (!(await PuedeInstitucionAsync(model.Id_Institucion) && await PuedeClaseAsync(model.Id_Clase) && await PuedeLicenciaAsync(model.Id_Licencia_Individual))) return SinPermisoJson();
            if (!ModelState.IsValid)
                return Json(new { exito = false, mensaje = "Datos inválidos. Revisa el formulario." });

            var (exito, mensaje) = await _masterDBService.CrearAlumnoMasterAsync(model);
            return Json(new { exito, mensaje });
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerFormularioEditarAlumnoMaster(int idUsuario)
        {
            if (!(await PuedeUsuarioAsync(idUsuario))) return SinPermisoHtml();
            var model = await _masterDBService.ObtenerDatosEditarAlumnoMasterAsync(idUsuario);
            if (model != null) model.ClasesDisponibles = await FiltrarClasesAsync(model.ClasesDisponibles);
            if (model == null) return NotFound();
            return PartialView("~/Views/Shared/Partials/Panels/Master/_EditarAlumnoMaster.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> GuardarEdicionAlumnoMaster(EditarAlumnoMasterModel model)
        {
            if (!(RolPermitido(model.Id_Rol) && await PuedeUsuarioAsync(model.Id_Usuario) && await PuedeClaseAsync(model.Id_Clase_Nueva ?? 0) && await PuedeLicenciaAsync(model.Id_Licencia_Individual))) return SinPermisoJson();
            var (exito, mensaje) = await _masterDBService.GuardarEdicionAlumnoMasterAsync(model);
            return Json(new { exito, mensaje });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleActivoAlumno(int idUsuario)
        {
            if (!(await PuedeUsuarioAsync(idUsuario))) return SinPermisoJson();
            var (exito, mensaje) = await _masterDBService.ToggleActivoAlumnoAsync(idUsuario);
            return Json(new { exito, mensaje });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleActivoClase(int idClase)
        {
            if (!(await PuedeClaseAsync(idClase))) return SinPermisoJson();
            var (exito, mensaje) = await _masterDBService.ToggleActivoClaseAsync(idClase);
            return Json(new { exito, mensaje });
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerTabProfesores(int idInstitucion)
        {
            if (!(await PuedeInstitucionAsync(idInstitucion))) return SinPermisoHtml();
            var model = await _masterDBService.ObtenerProfesoresInstitucionAsync(idInstitucion);
            return PartialView("~/Views/Shared/Partials/Panels/Master/_ListadoProfesMaster.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerFichaProfesorMaster(int idUsuario)
        {
            if (!(await PuedeUsuarioAsync(idUsuario))) return SinPermisoHtml();
            var model = await _masterDBService.ObtenerFichaAlumnoAsync(idUsuario); // mismo método, mismo modelo
            if (model == null) return NotFound();
            return PartialView("~/Views/Shared/Partials/Panels/Master/_FichaAlumnoMaster.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerFormularioCrearProfesor(int idInstitucion)
        {
            if (!(await PuedeInstitucionAsync(idInstitucion))) return SinPermisoHtml();
            var model = await _masterDBService.ObtenerFormularioCrearProfesorAsync(idInstitucion);
            model.ClasesDisponibles = await FiltrarClasesAsync(model.ClasesDisponibles);
            return PartialView("~/Views/Shared/Partials/Panels/Master/_CrearProfesorMaster.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> CrearProfesorMaster(CrearProfesorMasterModel model)
        {
            if (!(await PuedeInstitucionAsync(model.Id_Institucion) && await PuedeClasesAsync(model.Ids_Clases) && await PuedeLicenciaAsync(model.Id_Licencia_Individual))) return SinPermisoJson();
            if (!ModelState.IsValid)
                return Json(new { exito = false, mensaje = "Datos inválidos." });
            var (exito, mensaje) = await _masterDBService.CrearProfesorMasterAsync(model);
            return Json(new { exito, mensaje });
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerFormularioEditarProfesorMaster(int idUsuario)
        {
            if (!(await PuedeUsuarioAsync(idUsuario))) return SinPermisoHtml();
            var model = await _masterDBService.ObtenerDatosEditarProfesorMasterAsync(idUsuario);
            if (model != null) model.ClasesDisponibles = await FiltrarClasesAsync(model.ClasesDisponibles);
            if (model == null) return NotFound();
            return PartialView("~/Views/Shared/Partials/Panels/Master/_EditarProfesorMaster.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> GuardarEdicionProfesorMaster(EditarProfesorMasterModel model)
        {
            if (!(RolPermitido(model.Id_Rol) && await PuedeUsuarioAsync(model.Id_Usuario) && await PuedeClasesAsync(model.Ids_Clases_Nuevas) && await PuedeLicenciaAsync(model.Id_Licencia_Individual))) return SinPermisoJson();
            var (exito, mensaje) = await _masterDBService.GuardarEdicionProfesorMasterAsync(model);
            return Json(new { exito, mensaje });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleActivoProfesor(int idUsuario)
        {
            if (!(await PuedeUsuarioAsync(idUsuario))) return SinPermisoJson();
            var (exito, mensaje) = await _masterDBService.ToggleActivoProfesorAsync(idUsuario);
            return Json(new { exito, mensaje });
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerTabAdmins(int idInstitucion)
        {
            if (!(await PuedeInstitucionAsync(idInstitucion))) return SinPermisoHtml();
            var model = await _masterDBService.ObtenerAdminsInstitucionAsync(idInstitucion);
            return PartialView("~/Views/Shared/Partials/Panels/Master/_ListadoAdminsMaster.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerFormularioCrearAdmin(int idInstitucion)
        {
            if (!(await PuedeInstitucionAsync(idInstitucion))) return SinPermisoHtml();
            var model = await _masterDBService.ObtenerFormularioCrearAdminAsync(idInstitucion);
            model.ClasesDisponibles = await FiltrarClasesAsync(model.ClasesDisponibles);
            return PartialView("~/Views/Shared/Partials/Panels/Master/_CrearAdminMaster.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> CrearAdminMaster(CrearAdminMasterModel model)
        {
            if (!(RolPermitido(model.Id_Rol) && await PuedeInstitucionAsync(model.Id_Institucion) && await PuedeClasesAsync(model.Ids_Clases) && await PuedeLicenciaAsync(model.Id_Licencia_Individual))) return SinPermisoJson();
            if (!ModelState.IsValid)
                return Json(new { exito = false, mensaje = "Datos inválidos." });
            var (exito, mensaje) = await _masterDBService.CrearAdminMasterAsync(model);
            return Json(new { exito, mensaje });
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerFormularioEditarAdminMaster(int idUsuario)
        {
            if (!(await PuedeUsuarioAsync(idUsuario))) return SinPermisoHtml();
            var model = await _masterDBService.ObtenerDatosEditarAdminMasterAsync(idUsuario);
            if (model != null) model.ClasesDisponibles = await FiltrarClasesAsync(model.ClasesDisponibles);
            if (model == null) return NotFound();
            return PartialView("~/Views/Shared/Partials/Panels/Master/_EditarAdminMaster.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> GuardarEdicionAdminMaster(EditarAdminMasterModel model)
        {
            if (!(RolPermitido(model.Id_Rol) && await PuedeUsuarioAsync(model.Id_Usuario) && await PuedeClasesAsync(model.Ids_Clases_Nuevas) && await PuedeLicenciaAsync(model.Id_Licencia_Individual))) return SinPermisoJson();
            var (exito, mensaje) = await _masterDBService.GuardarEdicionAdminMasterAsync(model);
            return Json(new { exito, mensaje });
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerFichaAdminMaster(int idUsuario)
        {
            if (!(await PuedeUsuarioAsync(idUsuario))) return SinPermisoHtml();
            var model = await _masterDBService.ObtenerFichaAlumnoAsync(idUsuario);
            if (model == null) return NotFound();
            return PartialView("~/Views/Shared/Partials/Panels/Master/_FichaAlumnoMaster.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleActivoAdmin(int idUsuario)
        {
            if (!(await PuedeUsuarioAsync(idUsuario))) return SinPermisoJson();
            var (exito, mensaje) = await _masterDBService.ToggleActivoAdminAsync(idUsuario);
            return Json(new { exito, mensaje });
        }

        [HttpPost]
        public async Task<IActionResult> EliminarUsuario(int idUsuario)
        {
            if (!(await PuedeUsuarioAsync(idUsuario))) return SinPermisoJson();
            var (exito, mensaje) = await _masterDBService.EliminarUsuarioAsync(idUsuario);
            return Json(new { exito, mensaje });
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerFormularioEliminarClase(int idClase)
        {
            if (!(await PuedeClaseAsync(idClase))) return SinPermisoHtml();
            var model = await _masterDBService.ObtenerFormularioEliminarClaseAsync(idClase);
            if (model == null) return NotFound();
            return PartialView("~/Views/Shared/Partials/Panels/Master/_EliminarClase.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> EliminarClase(EliminarClaseModel model)
        {
            if (!(await PuedeClaseAsync(model.Id_Clase) && await PuedeClaseAsync(model.Id_Clase_Destino))) return SinPermisoJson();
            var (exito, mensaje) = await _masterDBService.EliminarClaseAsync(model);
            return Json(new { exito, mensaje });
        }

        [HttpGet]
        [Authorize(Roles = ROL_MASTER)]
        public async Task<IActionResult> ObtenerFormularioMoverUsuario(int idUsuario)
        {
            var model = await _masterDBService.ObtenerFormularioMoverUsuarioAsync(idUsuario);
            if (model == null) return NotFound();
            return PartialView("~/Views/Shared/Partials/Panels/Master/_MoverUsuario.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerClasesPorInstitucion(int idInstitucion)
        {
            if (!(await PuedeInstitucionAsync(idInstitucion))) return SinPermisoJson();
            var clases = await _masterDBService.ObtenerClasesPorInstitucionAsync(idInstitucion);
            return Json(clases);
        }

        [HttpPost]
        [Authorize(Roles = ROL_MASTER)]
        public async Task<IActionResult> MoverUsuario(MoverUsuarioModel model)
        {
            var (exito, mensaje) = await _masterDBService.MoverUsuarioAsync(model);
            return Json(new { exito, mensaje });
        }

        [HttpGet]
        [Authorize(Roles = ROL_MASTER)]
        public async Task<IActionResult> ObtenerFormularioMoverClase(int idClase)
        {
            var model = await _masterDBService.ObtenerFormularioMoverClaseAsync(idClase);
            if (model == null) return NotFound();
            return PartialView("~/Views/Shared/Partials/Panels/Master/_MoverClase.cshtml", model);
        }

        [HttpPost]
        [Authorize(Roles = ROL_MASTER)]
        public async Task<IActionResult> MoverClase(MoverClaseModel model)
        {
            var (exito, mensaje) = await _masterDBService.MoverClaseAsync(model);
            return Json(new { exito, mensaje });
        }

        [HttpPost]
        [Authorize(Roles = ROL_MASTER)]
        public async Task<IActionResult> EliminarInstitucion(int idInstitucion)
        {
            var (exito, mensaje) = await _masterDBService.EliminarInstitucionAsync(idInstitucion);
            return Json(new { exito, mensaje });
        }

        [HttpPost]
        [Authorize(Roles = ROL_MASTER)]
        public async Task<IActionResult> ToggleActivoInstitucion(int idInstitucion)
        {
            var (exito, mensaje) = await _masterDBService.ToggleActivoInstitucionAsync(idInstitucion);
            return Json(new { exito, mensaje });
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerTabConfiguracion(int idInstitucion)
        {
            if (!(await PuedeInstitucionAsync(idInstitucion))) return SinPermisoHtml();
            var model = await _masterDBService.ObtenerConfiguracionModulosAsync(idInstitucion);
            return PartialView("~/Views/Shared/Partials/Panels/Master/_ConfiguracionModulos.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> GuardarConfiguracionModulos(int idInstitucion, List<string> modulosActivos)
        {
            if (!(await PuedeInstitucionAsync(idInstitucion))) return SinPermisoJson();
            var (exito, mensaje) = await _masterDBService.GuardarConfiguracionModulosAsync(
                idInstitucion, modulosActivos ?? new List<string>());
            return Json(new { exito, mensaje });
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerLicenciasInstitucion(int idInstitucion)
        {
            if (!(await PuedeInstitucionAsync(idInstitucion))) return SinPermisoJson();
            var licencias = await _masterDBService.ObtenerLicenciasDisponiblesAsync(idInstitucion);
            return Json(licencias.Select(l => new
            {
                id = l.Id,
                tipoLicencia = l.TipoLicencia,
                fechaVencimiento = l.Fecha_Vencimiento.ToString("dd/MMM/yyyy"),
                diasRestantes = l.DiasRestantes,
                libre = l.Libre,
                nombreUsuarioActual = l.NombreUsuarioActual
            }));
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerTabLicencias(int idInstitucion)
        {
            if (!(await PuedeInstitucionAsync(idInstitucion))) return SinPermisoHtml();
            var model = await _masterDBService.ObtenerTabLicenciasAsync(idInstitucion);
            return PartialView("~/Views/Shared/Partials/Panels/Master/_TabLicencias.cshtml", model);
        }

        [HttpPost]
        [Authorize(Roles = ROL_MASTER)]
        public async Task<IActionResult> CrearLicenciaIndividual(CrearLicenciaIndividualModel model)
        {
            var (exito, mensaje) = await _masterDBService.CrearLicenciaIndividualAsync(model);
            return Json(new { exito, mensaje });
        }

        [HttpPost]
        [Authorize(Roles = ROL_MASTER)]
        public async Task<IActionResult> EliminarLicenciaIndividual(int idLicencia)
        {
            if (!(await PuedeLicenciaAsync(idLicencia))) return SinPermisoJson();
            var (exito, mensaje) = await _masterDBService.EliminarLicenciaIndividualAsync(idLicencia);
            return Json(new { exito, mensaje });
        }

        [HttpPost]
        [Authorize(Roles = ROL_MASTER)]
        public async Task<IActionResult> RenovarLicenciaIndividual(RenovarLicenciaModel model)
        {
            var (exito, mensaje) = await _masterDBService.RenovarLicenciaIndividualAsync(model);
            return Json(new { exito, mensaje });
        }

        [HttpPost]
        [Authorize(Roles = ROL_MASTER)]
        public async Task<IActionResult> DesasignarLicenciaManual(int idLicencia)
        {
            if (!(await PuedeLicenciaAsync(idLicencia))) return SinPermisoJson();
            var (exito, mensaje) = await _masterDBService.DesasignarLicenciaManualAsync(idLicencia);
            return Json(new { exito, mensaje });
        }


    }
}
