namespace AnzanMegaArithmetics.Models
{
    public class ResDadosViewModel
    {
        public List<EjercicioDadosModel> Ejercicios { get; set; }
        public List<RespuestaDadosModel> Respuestas { get; set; }
        public ConfDadosModel Configuracion { get; set; }
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
