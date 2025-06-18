using System.ComponentModel.DataAnnotations;

namespace AnzanMegaArithmetics.Models
{
    public class ConfMultiModel
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
        public string DigitosMultiplicando { get; set; } = string.Empty;
        [Required]
        public string DigitosMultiplicador { get; set; } = string.Empty;
        [Required]
        public int TiempoMeditacion { get; set; }
    }
}
