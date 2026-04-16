using System.ComponentModel.DataAnnotations;

namespace AnzanMegaArithmetics.Models.MasterModels
{
    public class EditarClaseModel
    {
        [Required]
        public int Id_Clase { get; set; }

        public string NombreActual { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre no puede estar vacío.")]
        [MinLength(2)]
        [MaxLength(100)]
        public string NuevoNombre { get; set; } = string.Empty;
    }
}
