using System.ComponentModel.DataAnnotations;

namespace AnzanMegaArithmetics.Models
{
    public class ConfNumeroFigura
    {
        [Required]
        [Display(Name = "Número de ejercicios:")]
        [Range(1, 100)]
        public int? CantidadEjercicios { get; set; } = 5;

        [Required]
        [Display(Name = "Número de dígitos:")]
        [Range(1, 10)]
        public int? NumeroDigitos { get; set; } = 1;

        [Required]
        [Display(Name = "Tipo de pregunta:")]
        public string TipoPregunta { get; set; } = "NumFig";

        [Required]
        [Display(Name = "Velocidad por ejercicio:")]
        public string VelocidadPreguntas { get; set; } = "0";

        [Required]
        [Display(Name = "Tiempo de concentración:")]
        [Range(0, 10)]
        public int? TiempoMeditacion { get; set; } = 0;
    }
}
