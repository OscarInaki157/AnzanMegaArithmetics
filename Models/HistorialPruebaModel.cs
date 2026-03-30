namespace AnzanMegaArithmetics.Models
{
    public class HistorialPruebaModel
    {
        public int Id_Prueba { get; set; }
        public DateTime Fecha { get; set; }
        public string NombreAlumno { get; set; }
        public string GamerTag { get; set; }
        public string TipoPrueba { get; set; }
        public int Aciertos { get; set; }
        public int TotalPreguntas { get; set; }
        public int Efectividad { get; set; } // Porcentaje
        public int XP { get; set; }
        public TimeSpan Tiempo { get; set; }
    }
}
