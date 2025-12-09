using System.ComponentModel.DataAnnotations;

namespace AnzanMegaArithmetics.Models
{
    public class ConfFlashModel
    {
        [Required]
        public int CantidadEjercicios { get; set; }
        [Required]
        public int MinDigitos { get; set; }
        [Required]
        public int MaxDigitos { get; set; }
        [Required]
        public string VelocidadPreguntas { get; set; } = string.Empty;
        [Required]
        public string TipoOperacion { get; set; } = string.Empty;
        [Required]
        public string DigitosSuma { get; set; } = string.Empty;
        [Required]
        public string DigitosResta { get; set; } = string.Empty;
        [Required]
        public int TiempoMeditacion { get; set; }
        [Required]
        public bool ActivarSonido { get; set; } = true;
        public string ColorA { get; set; } = string.Empty;
        public string ColorB { get; set; } = string.Empty;
        [Required]
        public bool ActivarDictado { get; set; } = false;
        [Required]
        public bool MostrarNumeros { get; set; } = true;
        [Required]
        public string ModoFlash { get; set; } = string.Empty;
    }
}
