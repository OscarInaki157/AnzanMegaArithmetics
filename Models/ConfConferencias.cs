using System.ComponentModel.DataAnnotations;

namespace AnzanMegaArithmetics.Models
{
    public class ConfConferencias
    {
        [Required]
        [Display(Name = "Estilo:")]
        public int Estilo { get; set; }
    }
}
