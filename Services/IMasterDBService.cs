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
    }
}
