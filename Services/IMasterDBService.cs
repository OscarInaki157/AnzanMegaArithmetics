using AnzanMegaArithmetics.Models;
using AnzanMegaArithmetics.Models.MasterModels;

namespace AnzanMegaArithmetics.Services
{
    public interface IMasterDBService
    {
        Task<PanelMasterViewModel> ObtenerDatosDashboardAsync();
        Task<(bool Exito, string Mensaje)> CrearInstitucionAsync(CrearInstitucionModel model);
        Task<GestionLicenciasModel> ObtenerDatosLicenciasAsync(int idInstitucion);
        Task<(bool Exito, string Mensaje)> ActualizarLicenciasAsync(GestionLicenciasModel model);
        Task<DetalleInstitucionViewModel> ObtenerDetalleInstitucionAsync(int id);
        Task<EditarInstitucionModel> ObtenerInstitucionParaEdicionAsync(int id);
        Task<(bool Exito, string Mensaje)> EditarNombreInstitucionAsync(int id, string nuevoNombre);

        Task<(bool Exito, string Mensaje)> CrearClaseAsync(CrearClaseModel model);
        Task<EditarClaseModel?> ObtenerClaseParaEdicionAsync(int idClase);
        Task<(bool Exito, string Mensaje)> EditarNombreClaseAsync(EditarClaseModel model);

        Task<ListadoAlumnosInstitucionViewModel> ObtenerAlumnosInstitucionAsync(int idInstitucion, string clase = "Todas");
        Task<UsuarioBDModel> ObtenerFichaAlumnoAsync(int idUsuario);
        Task<CrearAlumnoMasterModel> ObtenerFormularioCrearAlumnoAsync(int idInstitucion);
        Task<(bool Exito, string Mensaje)> CrearAlumnoMasterAsync(CrearAlumnoMasterModel model);
        Task<EditarAlumnoMasterModel> ObtenerDatosEditarAlumnoMasterAsync(int idUsuario);
        Task<(bool Exito, string Mensaje)> GuardarEdicionAlumnoMasterAsync(EditarAlumnoMasterModel model);
        Task<(bool Exito, string Mensaje)> ToggleActivoAlumnoAsync(int idUsuario);
        Task<(bool Exito, string Mensaje)> ToggleActivoClaseAsync(int idClase);

        Task<ListadoProfesoresInstitucionViewModel> ObtenerProfesoresInstitucionAsync(int idInstitucion);
        Task<CrearProfesorMasterModel> ObtenerFormularioCrearProfesorAsync(int idInstitucion);
        Task<(bool Exito, string Mensaje)> CrearProfesorMasterAsync(CrearProfesorMasterModel model);
        Task<EditarProfesorMasterModel> ObtenerDatosEditarProfesorMasterAsync(int idUsuario);
        Task<(bool Exito, string Mensaje)> GuardarEdicionProfesorMasterAsync(EditarProfesorMasterModel model);
        Task<(bool Exito, string Mensaje)> ToggleActivoProfesorAsync(int idUsuario);
    }
}
