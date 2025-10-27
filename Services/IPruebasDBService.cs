using AnzanMegaArithmetics.Models;

namespace AnzanMegaArithmetics.Services
{
    public interface IPruebasDBService
    {
        public List<PruebasDBModel> ObtenerPruebas();
        public bool GuardarPrueba(PruebasDBModel model);
    }
}
