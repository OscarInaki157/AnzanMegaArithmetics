using System.ComponentModel.DataAnnotations;

namespace AnzanMegaArithmetics.Models
{
    public class ConfDivModel
    {
        [Required]
        public int CantidadEjercicios { get; set; }
        [Required]
        public string FormatoPregunta { get; set; } = string.Empty;
        [Required]
        public string DireccionRespuesta { get; set; } = string.Empty;
        [Required]
        public string VelocidadPreguntas { get; set; } = string.Empty;
        [Required]
        public string DigitosDividendo { get; set; } = string.Empty;
        [Required]
        public string DigitosDivisor { get; set; } = string.Empty;
        [Required]
        public int TiempoMeditacion { get; set; }
        [Required]
        public string TipoEjercicio { get; set; } = string.Empty;
    }
}
