namespace AnzanMegaArithmetics.Models.MasterModels
{
    public class ListadoProfesoresInstitucionViewModel
    {
        public int Id_Institucion { get; set; }
        public List<UsuarioBDModel> ListaProfesores { get; set; } = new();
    }
}
