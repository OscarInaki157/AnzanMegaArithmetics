namespace AnzanMegaArithmetics.Models.MasterModels
{
    public class GestionLicenciasModel
    {
        public int Id_Institucion { get; set; }
        public string NombreInstitucion { get; set; }
        public int LicenciasTotales { get; set; }
        public int LicenciasUsadas { get; set; }

        // El nuevo valor que el Master escribirá
        public int NuevasLicenciasTotales { get; set; }
    }
}
