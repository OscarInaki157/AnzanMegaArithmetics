namespace AnzanMegaArithmetics.Models.MasterModels
{
    public class InstitucionDirectorioModel
    {
        public int Id_Institucion { get; set; }
        public string Nombre { get; set; }
        public bool Activo { get; set; }
        //public string Director { get; set; }
        public int LicenciasTotales { get; set; }
        public int LicenciasUsadas { get; set; }
        public DateTime FechaRegistro { get; set; }
    }
}
