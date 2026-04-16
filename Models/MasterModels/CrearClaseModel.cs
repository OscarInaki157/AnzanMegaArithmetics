using System.ComponentModel.DataAnnotations;

namespace AnzanMegaArithmetics.Models.MasterModels
{
    public class CrearClaseModel
    {
        [Required]
        public int Id_Institucion { get; set; }

        [Required(ErrorMessage = "El nombre del salón es obligatorio.")]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;
    }
}
