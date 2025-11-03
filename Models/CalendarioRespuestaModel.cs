namespace AnzanMegaArithmetics.Models
{
    public class CalendarioRespuestaModel
    {
        public int Id_Ejercicio { get; set; }
        public string Respuesta_Usuario { get; set; }
        public bool Es_Correcto { get; set; }
        public double Tiempo_Respuesta { get; set; }
    }
}
