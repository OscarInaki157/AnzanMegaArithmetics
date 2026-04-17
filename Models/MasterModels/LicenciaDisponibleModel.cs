namespace AnzanMegaArithmetics.Models.MasterModels
{
    public class LicenciaDisponibleModel
    {
        public int Id { get; set; }
        public string TipoLicencia { get; set; } = string.Empty;
        public DateTime Fecha_Compra { get; set; }
        public DateTime Fecha_Vencimiento { get; set; }
        public bool Libre { get; set; }
        public int? Id_Usuario_Actual { get; set; }
        public string NombreUsuarioActual { get; set; } = string.Empty;
        public int DiasRestantes => Math.Max(0, (Fecha_Vencimiento - DateTime.Now).Days);
    }
}
