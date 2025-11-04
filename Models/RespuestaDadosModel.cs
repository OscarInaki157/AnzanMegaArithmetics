namespace AnzanMegaArithmetics.Models
{
    public class RespuestaDadosModel
    {
        public int Id_Ejercicio { get; set; }
        public string Respuesta_Usuario { get; set; }
        public bool Es_Correcta { get; set; }
        public double Tiempo_Respuesta { get; set; }
        public int DadosUtilizados { get; set; }
    }
}
