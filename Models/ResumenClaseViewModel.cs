namespace AnzanMegaArithmetics.Models
{
    public class ResumenClaseViewModel
    {
        public int Id_Usuario { get; set; }
        public string Id_Rol { get; set; }
        public string Nombre { get; set; }
        public string Correo { get; set; }
        public string Gamer_Tag { get; set; }
        public int Exp { get; set; } 
        public string Licencia { get; set; } = string.Empty; 
        public DateTime? Ultima_Cnx { get; set; }
        public List<string> Clases { get; set; } = new();


        public List<string> ClasesDisponibles { get; set; } = new();
        public string ClaseActual { get; set; } = string.Empty;

        public int TotalAlumnosActivos { get; set; }
        public int TotalPruebasRealizadas { get; set; }
        public double PromedioGeneralClase { get; set; }


        public Dictionary<string, double> RendimientoPorActividad { get; set; } = new();

        public Dictionary<string, int> DistribucionPruebas { get; set; } = new();

        public List<UsuarioBDModel> ListaAlumnos { get; set; } = new();
        public List<string> TiposDePruebaDisponibles { get; set; } = new();
    }
}
