using System.ComponentModel.DataAnnotations;

namespace AnzanMegaArithmetics.Models
{
    public class LoginResponseModel
    {
        [Required]
        public int Id_Usuario { get; set; }
        [Required]
        public string Clase { get; set; } = string.Empty;
        [Required]
        public string Nombre { get; set; } = string.Empty;
        [Required]
        public string Usuario { get; set; } = string.Empty;
        [Required]
        public string Rol { get; set; } = string.Empty;
    }
}
