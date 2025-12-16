namespace AnzanMegaArithmetics.Models
{
    public class ActualizarUsuarioModel
    {
        public int Id_Usuario { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Gamer_Tag { get; set; } = string.Empty;
        public string Pass { get; set; } = string.Empty;
        public int Id_Rol { get; set; }
        public bool Activo { get; set; }
        public int Exp { get; set; }
        public int Racha { get; set; }
        public string Licencia { get; set; } = string.Empty;

        public string Rango_Actual { get; set; } = string.Empty;

        //public DateTime? Fecha_Vencimiento_Licencia { get; set; }

        public List<int> ClasesSeleccionadas { get; set; } = new List<int>();

    }
}
