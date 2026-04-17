namespace AnzanMegaArithmetics.Models.MasterModels
{
    public class ListadoAdminsViewModel
    {
        public int Id_Institucion { get; set; }
        public List<UsuarioBDModel> ListaAdmins { get; set; } = new();
    }
}
