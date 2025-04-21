using System.ComponentModel.DataAnnotations;

namespace AnzanMegaArithmetics.Models
{
    public class ConfSumaRestaModel
    {
        [Required]
        public int CantidadEjercicios { get; set; }
        [Required]
        public int NumeroOperaciones { get; set; }
        [Required]
        public int MinDigitos { get; set; }
        [Required]
        public int MaxDigitos { get; set; }
        [Required]
        public string VelocidadPreguntas { get; set; } = string.Empty;
        [Required]
        public int ValorMaximo { get; set; }
        [Required]
        public bool UsarMaximoComoBase { get; set; }
        [Required]
        public string TipoOperacion { get; set; } = string.Empty;
        [Required]
        public string DigitosSuma { get; set; } = string.Empty;
        [Required]
        public string DigitosResta { get; set; } = string.Empty;
        [Required]
        public bool DirectaSuma { get; set; }
        [Required]
        public bool DirectaResta { get; set; }
        [Required]
        public int TiempoMeditacion { get; set; }
    }
}
