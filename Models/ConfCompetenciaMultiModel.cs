namespace AnzanMegaArithmetics.Models
{
    public class ConfCompetenciaMultiModel
    {
        public string TipoPregunta { get; set; } = string.Empty;
        public string DireccionRespuesta { get; set; } = string.Empty;
        public string FormatoPregunta { get; set; } = string.Empty;
        public bool MostrarContadorTiempo { get; set; } = true;
        public int TiempoMeditacion { get; set; }
    }
}
