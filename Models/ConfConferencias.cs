using System.ComponentModel.DataAnnotations;

namespace AnzanMegaArithmetics.Models
{
    public class ConfConferencias
    {
        [Required]
        [Display(Name = "Color:")]
        public int Color { get; set; }
    }
}
