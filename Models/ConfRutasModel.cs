using System.ComponentModel.DataAnnotations;

namespace AnzanMegaArithmetics.Models
{
    public class ConfRutasModel
    {
        [Required]
        public int CantidadEjercicios { get; set; }
        [Required]
        public int ValorInicial { get; set; }
        [Required]
        public int ValorFinal { get; set; }
        [Required]
        public string Modalidad { get; set; } = string.Empty;
        [Required]
        public string CategoriaEjercicios { get; set; } = string.Empty;
        [Required]
        public string VelocidadPreguntas { get; set; } = string.Empty;
        [Required]
        public string TipoPregunta { get; set; } = string.Empty;
        [Required]
        public string TipoRespuesta { get; set; } = string.Empty;
        [Required]
        public int TiempoMeditacion { get; set; }
    }
}
