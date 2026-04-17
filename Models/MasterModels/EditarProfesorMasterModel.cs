using System.ComponentModel.DataAnnotations;

namespace AnzanMegaArithmetics.Models.MasterModels
{
    public class EditarProfesorMasterModel
    {
        public int Id_Usuario { get; set; }

        [Required, MaxLength(80), MinLength(3)]
        public string Nombre { get; set; } = string.Empty;

        [Required, MaxLength(20), MinLength(3)]
        public string Gamer_Tag { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Correo { get; set; } = string.Empty;

        [Required, MaxLength(30), MinLength(6)]
        public string Pass { get; set; } = string.Empty;

        public int Racha { get; set; }
        public int Exp { get; set; }
        public bool Activo { get; set; }

        // Clases actuales (IDs)
        public List<int> Ids_Clases_Actuales { get; set; } = new();

        // Clases seleccionadas en el form
        public List<int> Ids_Clases_Nuevas { get; set; } = new();

        // Para poblar checkboxes
        public List<ClaseSedeModel> ClasesDisponibles { get; set; } = new();

        // Licencia
        public string LicenciaActual { get; set; } = "Sin licencia";
        public int? Id_UsuarioLicencia { get; set; }
        public DateTime? Fecha_Inicio_Licencia { get; set; }
        public DateTime? Fecha_Fin_Licencia { get; set; }
    }
}
