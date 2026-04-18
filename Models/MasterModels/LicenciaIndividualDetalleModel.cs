namespace AnzanMegaArithmetics.Models.MasterModels
{
    public class LicenciaIndividualDetalleModel
    {
        public int Id { get; set; }
        public string TipoLicencia { get; set; } = string.Empty;
        public DateTime Fecha_Compra { get; set; }
        public DateTime Fecha_Vencimiento { get; set; }
        public bool Activo { get; set; }
        public int? Id_Usuario { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string CorreoUsuario { get; set; } = string.Empty;
        public int DiasRestantes => Math.Max(0, (Fecha_Vencimiento - DateTime.Now).Days);
        public bool Vencida => Fecha_Vencimiento < DateTime.Now;
        public bool Libre => Id_Usuario == null;
    }
}
