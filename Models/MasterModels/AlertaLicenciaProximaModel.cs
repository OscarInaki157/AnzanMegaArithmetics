namespace AnzanMegaArithmetics.Models.MasterModels
{
    public class AlertaLicenciaProximaModel
    {
        public string NombreUsuario { get; set; }
        public string NombreInstitucion { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public int DiasRestantes { get; set; }
    }
}
