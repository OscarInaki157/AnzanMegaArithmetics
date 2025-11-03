using System.ComponentModel.DataAnnotations;

namespace AnzanMegaArithmetics.Models
{
    public class CalendarioConfModel
    {
        [Required]
        [Range (0,100)]
        [Display(Name = "Cantidad de ejercicios: ")]
        public int CantidadEjercicios { get; set; } = 0;
        [Required]
        public string TipoOperacion { get; set; }
        [Required]
        public int AnnoInicio { get; set; }
        [Required]
        public int AnnoFin { get; set; }
        [Required]
        [Range(0,40)]
        [Display(Name = "Minutos para contestar todos los ejercicios: ")]
        public int TiempoLimiteMinutos { get; set; }
        [Required]
        [Range(0,10)]
        [Display(Name = "Tiempo de concentración: ")]
        public int TiempoConcentracion { get; set; }
        public string TipoPrueba { get; set; } = string.Empty;
    }
}
