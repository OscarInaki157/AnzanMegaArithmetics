using System.ComponentModel.DataAnnotations;

namespace AnzanMegaArithmetics.Models
{
    public class ConfLecturaFingerModel
    {
        [Required]
        [Display(Name = "Número de ejercicios:")]
        [Range(1, 300)]
        public int CantidadEjercicios { get; set; }

        [Required]
        [Display(Name = "Tipo de ejercicios:")]
        public string TipoPregunta { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Velocidad por ejercicio:")]
        public string VelocidadPreguntas { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Tiempo de concentración:")]
        [Range(0, 10)]
        public int TiempoMeditacion { get; set; }

        [Display(Name = "Estilo de Manos:")]
        public int Estilo { get; set; }
    }

}
