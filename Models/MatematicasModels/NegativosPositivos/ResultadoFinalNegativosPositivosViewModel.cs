namespace AnzanMegaArithmetics.Models.MatematicasModels.NegativosPositivos
{
    public class ResultadoFinalNegativosPositivosViewModel
    {
        public List<ResultadoNegativosPositivos> Resultados { get; set; } = new();
        public int CantidadEjercicios { get; set; }
        public string TipoEjercicio { get; set; } = "clasico";
        public string VelocidadEjercicio { get; set; } = "0";
        public string TipoDigitos { get; set; } = "ambos";
        public int MinDigitos { get; set; }
        public int MaxDigitos { get; set; }

    }
}
