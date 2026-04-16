using AnzanMegaArithmetics.Models;

namespace AnzanMegaArithmetics.Models.MasterModels
{
    public class ListadoAlumnosInstitucionViewModel
    {
        public int Id_Institucion { get; set; }
        public List<UsuarioBDModel> ListaAlumnos { get; set; } = new();
        public List<string> ClasesDisponibles { get; set; } = new();
        public string ClaseActual { get; set; } = "Todas";
    }
}