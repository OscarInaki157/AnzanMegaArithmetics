using System.ComponentModel.DataAnnotations;

namespace AnzanMegaArithmetics.Models.MasterModels
{
    public class CrearAdminMasterModel
    {
        public int Id_Institucion { get; set; }

        [Required, MaxLength(80), MinLength(3)]
        public string Nombre { get; set; } = string.Empty;

        [Required, MaxLength(20), MinLength(3)]
        public string Gamer_Tag { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Correo { get; set; } = string.Empty;

        [Required, MaxLength(30), MinLength(6)]
        public string Pass { get; set; } = string.Empty;

        // Rol a asignar
        [Required]
        public int Id_Rol { get; set; } // 3 = Admin, 4 = Master

        public List<int> Ids_Clases { get; set; } = new();
        public List<ClaseSedeModel> ClasesDisponibles { get; set; } = new();

        public int Id_Licencia_Individual { get; set; }
        public List<LicenciaDisponibleModel> LicenciasDisponibles { get; set; } = new();
    }
}
