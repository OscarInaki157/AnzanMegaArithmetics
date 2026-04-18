namespace AnzanMegaArithmetics.Models.MasterModels
{
    public class CrearLicenciaIndividualModel
    {
        public int Id_Institucion { get; set; }
        public int MesesVigencia { get; set; } = 12;
        public int? Id_Usuario { get; set; }
    }
}
