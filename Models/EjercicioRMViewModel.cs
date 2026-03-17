namespace AnzanMegaArithmetics.Models
{
    public class EjercicioRMViewModel
    {
        public RRutaMModel EjercicioActual { get; set; }
        public ConfRutasModel Configuracion { get; set; }
        public int NumeroEjercicio { get; set; }
        public int TotalEjercicios { get; set; }
        public double VelocidadPregunta { get; set; }
    }
}
