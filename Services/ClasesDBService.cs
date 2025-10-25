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

        public int ListarClasesTotales()
        {
            int contador = 0;
            try
            {
                contador = _context.Clases.Count();
                return contador;
            }
            catch (Exception ex)
            {
                return contador;
            }
        }

        public List<ClaseBDModel> ObtenerClases()
        {
            List<ClaseBDModel> clasesModel = new List<ClaseBDModel>();
            try
            {
                List<ClaseDB> clases = _context.Clases.ToList();
                
                foreach (var clase in clases)
                {
                    clasesModel.Add(new ClaseBDModel
                    {
                        Id_Clase = clase.Id_Clase,
                        Nombre = clase.Nombre
                    });
                }

                return clasesModel;
            }
            catch (Exception ex)
            {
                return new List<ClaseBDModel>();
            }
        }

        public string ActualizarClase(ActualizarClaseModel model)
        {
            try
            {
                //actualizar nombre de la clase
                var claseToUpdate = _context.Clases.FirstOrDefault(c => c.Id_Clase == model.Id_Clase);
                if (claseToUpdate == null)
                {
                    return "Error: Clase no encontrada.";
                }
                claseToUpdate.Nombre = model.Nombre;
                _context.SaveChanges();
                return "Clase actualizada correctamente";
            }
            catch (Exception ex)
            {
                return "Error al actualizar la clase: " + ex.Message;
            }
        }

    }
}
