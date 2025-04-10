using System.ComponentModel.DataAnnotations;

namespace AnzanMegaArithmetics.Models
{
    public class ConfEscrituraFingerModel
    {
        [Display(Name = "Número de ejercicios:")]
        [Range(1, 300)]
        public int CantidadEjercicios { get; set; }

        [Display(Name = "Tipo de ejercicios:")]
        public string TipoPregunta { get; set; }

        [Display(Name = "Velocidad por ejercicio:")]
        public string VelocidadPreguntas { get; set; }

        [Display(Name = "Tiempo de concentración:")]
        [Range(0, 300)]
        public int TiempoMeditacion { get; set; }
    }
}
