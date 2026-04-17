namespace AnzanMegaArithmetics.Models.MasterModels
{
    public class ConfiguracionInstitucionViewModel
    {
        public int Id_Institucion { get; set; }
        public string NombreInstitucion { get; set; } = string.Empty;
        public HashSet<string> ModulosActivos { get; set; } = new();
    }
}
