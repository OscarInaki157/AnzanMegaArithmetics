namespace AnzanMegaArithmetics.Models
{
    public class ResPotenciasViewModel
    {
        public List<EjercicioPotenciasModel> Ejercicios { get; set; }
        public List<RespuestaPotenciaModel> Respuestas { get; set; }
        public ConfPotenciasModel Configuracion { get; set; }
        public int TotalCorrectas { get; set; }
        public int TotalIncorrectas { get; set; }
        public int TotalSinResponder { get; set; }
        public double PorcentajeAcierto { get; set; }
        public double TiempoPromedio { get; set; }
        public double TiempoTotal { get; set; }
        public DateTime FechaPrueba { get; set; }
        public int TotalEjercicios => Ejercicios?.Count ?? 0;
    }
}
