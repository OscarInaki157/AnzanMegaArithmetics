using System.ComponentModel.DataAnnotations;

namespace AnzanMegaArithmetics.Models
{
    public class ConfMemoriaFlashModel
    {
        [Required]
        public int CantidadEjercicios { get; set; }
        [Required]
        public string VelocidadPreguntas { get; set; } = string.Empty;
        [Required]
        public string CategoriaEjercicios { get; set; } = string.Empty;
        [Required]
        public string TipoPreguntas { get; set; } = string.Empty;
        [Required]
        public int DigitosEjercicios { get; set; }
        [Required]
        public string MostrarParejas { get; set; } = string.Empty;
        [Required]
        public int TiempoMeditacion { get; set; }
        [Required]
        public bool ActivarSonido { get; set; } = true;
        public string ColorA { get; set; } = string.Empty;
        public string ColorB { get; set; } = string.Empty;
    }
}
