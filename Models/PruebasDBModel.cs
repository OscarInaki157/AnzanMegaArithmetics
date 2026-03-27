using System.ComponentModel.DataAnnotations;

namespace AnzanMegaArithmetics.Models
{
    public class PruebasDBModel
    {
        public int Id_Prueba { get; set; }
        public int Id_Usuario { get; set; }
        public string Ids_Clases { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public TimeSpan Tiempo { get; set; }
        public int Total_Preguntas { get; set; } = 0;
        public int Respuestas_Correctas { get; set; } = 0;
        public DateTime Fecha { get; set; }
        public int ExperienciaAdquirida { get; set; } = 0;
        public string Tipo_Prueba { get; set; } = string.Empty;
        [MaxLength(500)]
        public string Configuracion { get; set; } = string.Empty;

        public string NombreUsuario { get; set; }
        public string GamertagUsuario { get; set; }
    }
}
