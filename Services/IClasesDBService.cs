using AnzanMegaArithmetics.Models;

namespace AnzanMegaArithmetics.Services
{
    public interface IClasesDBService
    {
        public int ListarClasesTotales();
        public List<ClaseBDModel> ObtenerClases();
        public string ActualizarClase(ActualizarClaseModel model);
    }
}
