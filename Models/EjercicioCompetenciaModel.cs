namespace AnzanMegaArithmetics.Models
{
    public class EjercicioCompetenciaModel
    {
        public string Multiplicando { get; set; }
        public string Multiplicador { get; set; }
        public string FormatoPregunta { get; set; }
        public string DireccionRespuesta { get; set; }
        public string RespuestaUsuario { get; set; } = string.Empty;
        public bool Respondido { get; set; } = false;
        public bool EsCorrecto { get; set; } = false;
        public TimeSpan TiempoRespuesta { get; set; }
    }
}
