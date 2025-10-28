using System.ComponentModel.DataAnnotations;

namespace AnzanMegaArithmetics.Models
{
    public class ConfCuadrosModel
    {
        [Required]
        [Display(Name = "Cantidad de rejillas: ")]
        [Range(1,10)]
        public int CantidadRejillas { get; set; }

        [Required]
        [Display(Name = "Dimensión de la(s) rejilla: ")]
        public string DimensionRejilla { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Tipo de iluminación: ")]
        public string TipoIluminacion { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Tipo de operación: ")]
        public string TipoOperacion { get; set; } = string.Empty;

        [Required]
        public string DigitosSuma { get; set; } = string.Empty;

        [Required]
        public string DigitosResta { get; set; } = string.Empty;

        [Required]
        public int TiempoMeditacion { get; set; }

        public string TipoPrueba { get; set; } = string.Empty;
    }
}
