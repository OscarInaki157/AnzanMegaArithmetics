using System.ComponentModel.DataAnnotations;

namespace AnzanMegaArithmetics.Models.MasterModels
{
    public class EditarAlumnoMasterModel
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

        // Clase actualmente asignada
        public int? Id_Clase_Actual { get; set; }

        // Nueva clase seleccionada (null = sin cambio)
        public int? Id_Clase_Nueva { get; set; }

        // Para poblar el dropdown
        public List<ClaseSedeModel> ClasesDisponibles { get; set; } = new();

        // Datos actuales de licencia (para mostrar)
        public string LicenciaActual { get; set; } = "Sin licencia";
        public int? Id_UsuarioLicencia { get; set; } // PK de Usuarios_Licencias para editar

        // Campos editables de licencia
        public DateTime? Fecha_Inicio_Licencia { get; set; }
        public DateTime? Fecha_Fin_Licencia { get; set; }

        public int Id_Rol { get; set; }
        public string NombreRol { get; set; } = string.Empty;
    }
}
