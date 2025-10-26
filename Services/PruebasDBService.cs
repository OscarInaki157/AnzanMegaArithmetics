using AnzanMegaArithmetics.Models;
using DataBase;

namespace AnzanMegaArithmetics.Services
{
    public class PruebasDBService : IPruebasDBService
    {
        private readonly AnzanMegaContext _context;
        public PruebasDBService(AnzanMegaContext context) 
        {
            this._context = context;
        }


        public List<PruebasDBModel> ObtenerPruebas()
        {
            try
            {
                List<PruebasDB> pruebasDB =_context.Pruebas.ToList();

                if (pruebasDB == null || pruebasDB.Count == 0)
                {
                    return new List<PruebasDBModel>();
                }

                List<PruebasDBModel> pruebasModel = new();
                foreach (var prueba in pruebasDB)
                {
                    PruebasDBModel modelo = new PruebasDBModel
                    {
                        Id_Prueba = prueba.Id_Prueba,
                        Id_Usuario = prueba.Id_Usuario,
                        Id_Clase = prueba.Id_Clase,
                        Activo = prueba.Activo,
                        Tiempo = prueba.Tiempo,
                        Total_Preguntas = prueba.Total_Preguntas,
                        Respuestas_Correctas = prueba.Respuestas_Correctas,
                        Fecha = prueba.Fecha,
                        ExperienciaAdquirida = prueba.ExperienciaAdquirida,
                        Tipo_Prueba = prueba.Tipo_Prueba
                    };
                    pruebasModel.Add(modelo);
                }
                return pruebasModel;
            }
            catch (Exception ex)
            {
                return new List<PruebasDBModel>();
            }
        }


    }
}
