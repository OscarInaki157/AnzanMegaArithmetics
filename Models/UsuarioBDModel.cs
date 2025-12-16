namespace AnzanMegaArithmetics.Models
{
    public class UsuarioBDModel
    {
        public int Id_Usuario { get; set; }
        public string Id_Rol { get; set; } //recuperemos el rol en la consulta linq
        public string Nombre { get; set; }
        public string Correo { get; set; }
        public string Gamer_Tag { get; set; }
        public string Pass { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public List<string> Clases { get; set; } = new List<string>(); //lista de clases del usuario
        public int Racha { get; set; }
        public int Exp { get; set; }
        public string Rango_Actual { get; set; }
        public DateTime Ultima_Cnx { get; set; }
        public string Licencia { get; set; } //nombre de la licencia activa del usuario linq obt

        public DateTime? Fecha_Asignacion_Licencia { get; set; }
        public DateTime? Fecha_Vencimiento_Licencia { get; set; }

    }
}
