namespace AnzanMegaArithmetics.Models.MasterModels
{
    public class GestionLicenciasIndividualViewModel
    {
        public int Id_Institucion { get; set; }
        public string NombreInstitucion { get; set; } = string.Empty;
        public int TotalLicencias { get; set; }
        public int LicenciasAsignadas { get; set; }
        public int LicenciasLibres { get; set; }
        public int LicenciasVencidas { get; set; }
        public List<LicenciaIndividualDetalleModel> Licencias { get; set; } = new();
    }
}
