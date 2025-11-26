using AnzanMegaArithmetics.Helpers;

namespace AnzanMegaArithmetics.Models
{
    public class DailyChallengeViewModel
    {
        public int IdReto { get; set; }
        public string Descripcion { get; set; }
        public int RecompensaXP { get; set; }
        public bool EsCompletado { get; set; }
        public TipoActividadEnum TipoActividad { get; set; }
        public int? ValorObjetivo { get; set; }
        public bool FueReclamado { get; set; }
    }
}
