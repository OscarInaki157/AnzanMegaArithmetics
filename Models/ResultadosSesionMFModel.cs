namespace AnzanMegaArithmetics.Models
{
    public class ResultadosSesionMFModel
    {
        public ConfMemoriaFlashModel Configuracion { get; set; }
        public int TotalEjercicios { get; set; }
        public int TotalAciertos { get; set; }
        public List<ResultadoEjercicioMFModel> ResultadosEjercicios { get; set; }
    }
}
