namespace AnzanMegaArithmetics.Models
{
    public class EjercicioRaicesModel
    {
        public  int Id_Ejercicio { get; set; }
        public int Numero_Base { get; set; }
        public double Respuesta_Correcta => Math.Sqrt(Numero_Base);
    }
}
