using AnzanMegaArithmetics.Models;
using DataBase;

namespace AnzanMegaArithmetics.Services
{
    public interface IPruebasDBService
    {
        public ConfiguracionPruebaModel ObtenerConfiguracionPorId(int idPrueba);
        public List<string> ObtenerTiposDePruebaDisponibles();
        public List<HistorialPruebaModel> ObtenerHistorialFiltrado(string clase, string alumno, string tipoPrueba, DateTime? fechaInicio, DateTime? fechaFin);
        public List<PruebasDBModel> ObtenerPruebas();
        public ResumenClaseViewModel ObtenerResumenPorClase(string claseSeleccionada);
        public bool GuardarPrueba(PruebasDBModel model);
        public void CalcularRacha(UsuariosDB usuario, DateTime fechaActual);
    }
}
