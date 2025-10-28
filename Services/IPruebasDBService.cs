using AnzanMegaArithmetics.Models;
using DataBase;

namespace AnzanMegaArithmetics.Services
{
    public interface IPruebasDBService
    {
        public List<PruebasDBModel> ObtenerPruebas();
        public bool GuardarPrueba(PruebasDBModel model);
        public void CalcularRacha(UsuariosDB usuario, DateTime fechaActual);
    }
}
