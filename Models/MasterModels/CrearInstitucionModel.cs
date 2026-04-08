using System.ComponentModel.DataAnnotations;

namespace AnzanMegaArithmetics.Models.MasterModels
{
    public class CrearInstitucionModel
    {
        [Required(ErrorMessage = "El nombre de la institución es obligatorio.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 100 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debes especificar las licencias iniciales.")]
        [Range(1, 500000, ErrorMessage = "La bolsa inicial debe ser de al menos 1 licencia.")]
        public int LicenciasIniciales { get; set; } = 1;
    }
}
