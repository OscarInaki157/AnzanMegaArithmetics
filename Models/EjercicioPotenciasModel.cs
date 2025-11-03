namespace AnzanMegaArithmetics.Models
{
    public class EjercicioPotenciasModel
    {
        public int Id_Ejercicio { get; set; }
        public int Numero_Base { get; set; }
        public double Respuesta_Correcta => Math.Pow(Numero_Base, 2);
    }
}
