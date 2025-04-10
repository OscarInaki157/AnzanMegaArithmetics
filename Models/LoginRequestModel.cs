using System.ComponentModel.DataAnnotations;

namespace AnzanMegaArithmetics.Models
{
    public class LoginRequestModel
    {
        [Required]
        public string Usuario { get; set; } = string.Empty;
        [Required]
        public string Pass { get; set; } = string.Empty;
    }
}
