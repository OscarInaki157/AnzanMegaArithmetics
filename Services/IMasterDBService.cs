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
    }
}
