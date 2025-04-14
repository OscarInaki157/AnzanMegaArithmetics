using System.ComponentModel.DataAnnotations;

namespace AnzanMegaArithmetics.Models
{
    public class ConfEscrituraSorobanModel
    {
        [Required]
        [Display(Name = "Número de ejercicios:")]
        [Range(1, 300)]
        public int CantidadEjercicios { get; set; }

        [Required]
        [Display(Name = "Valor mínimo:")]
        [Range(0, 9999999999)]
        public int VMinimo { get; set; }

        [Required]
        [Display(Name = "Valor máximo:")]
        [Range(0, 9999999999)]
        public int VMaximo { get; set; }

        [Required]
        [Display(Name = "Velocidad por ejercicio:")]
        public string VelocidadPreguntas { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Tiempo de concentración:")]
        [Range(0, 10)]
        public int TiempoMeditacion { get; set; }
    }
}
