using System.ComponentModel.DataAnnotations;

namespace AnzanMegaArithmetics.Models
{
    public class ConfTablasModel
    {
        [Required]
        public int CantidadEjercicios { get; set; } = 1;
        [Required]
        public string VelocidadPreguntas { get; set; } = string.Empty;
        [Required]
        public string TipoPregunta { get; set; } = string.Empty;
        [Required]
        public string ParImpar { get; set; } = string.Empty;
        [Required]
        public string DigitosMultiplicacion { get; set; } = string.Empty;
        [Required]
        public int TiempoMeditacion { get; set; }
    }
}
