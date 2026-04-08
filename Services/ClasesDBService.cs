using AnzanMegaArithmetics.Models;
using DataBase;

namespace AnzanMegaArithmetics.Services
{
    public class ClasesDBService : IClasesDBService
    {
        private readonly AnzanMegaContext _context;

        public ClasesDBService(AnzanMegaContext context)
        {
            this._context = context;
        }




    }
}
