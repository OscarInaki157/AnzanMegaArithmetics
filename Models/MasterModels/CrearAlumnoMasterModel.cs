using System.ComponentModel.DataAnnotations;

namespace AnzanMegaArithmetics.Models.MasterModels
{
    public class CrearAlumnoMasterModel
    {
        public int Id_Institucion { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [MaxLength(80), MinLength(3)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El GamerTag es obligatorio.")]
        [MaxLength(20), MinLength(3)]
        public string Gamer_Tag { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress]
        public string Correo { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [MaxLength(30), MinLength(6)]
        public string Pass { get; set; } = string.Empty;

        // Clase a la que se asignará
        [Required(ErrorMessage = "Debes seleccionar una clase.")]
        public int Id_Clase { get; set; }

        // Para poblar el dropdown en el formulario
        public List<ClaseSedeModel> ClasesDisponibles { get; set; } = new();
        public int LicenciasDisponibles { get; set; }
    }
}
