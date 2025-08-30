namespace AnzanMegaArithmetics.Models
{
    public class ResultadosCuadrosPruebaModel
    {
        public ConfCuadrosModel config { get; set; }
        public List<EjercicioCuadrosModel> ejercicios { get; set; }
        public List<RCuadrosModel> respuestas { get; set; }
    }
}
