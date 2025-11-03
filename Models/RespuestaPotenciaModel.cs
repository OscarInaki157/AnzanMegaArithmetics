namespace AnzanMegaArithmetics.Models
{
    public class RespuestaPotenciaModel
    {
        public int Id_Ejercicio { get; set; }
        public double Respuesta_Usuario { get; set; }
        public bool Es_Correcto { get; set; }
        public double Tiempo_Respuesta { get; set; }
    }
}
