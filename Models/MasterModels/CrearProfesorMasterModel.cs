using System.ComponentModel.DataAnnotations;

namespace AnzanMegaArithmetics.Models.MasterModels
{
    public class CrearProfesorMasterModel
    {
        public int Id_Institucion { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [MaxLength(80), MinLength(3)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El GamerTag es obligatorio.")]
        [MaxLength(20), MinLength(3)]
        public string Gamer_Tag { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Correo { get; set; } = string.Empty;

        [Required, MaxLength(30), MinLength(6)]
        public string Pass { get; set; } = string.Empty;

        // Múltiples clases
        public List<int> Ids_Clases { get; set; } = new();

        // Para poblar checkboxes
        public List<ClaseSedeModel> ClasesDisponibles { get; set; } = new();

        // Para bloquear si no hay licencias
        public int LicenciasDisponibles { get; set; }
    }
}
