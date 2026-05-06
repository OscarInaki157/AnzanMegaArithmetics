namespace AnzanMegaArithmetics.Models.MatematicasModels.LeyesSignosOperaciones
{
    public class ResultadoFinalLeyesSignosOperacionesViewModel
    {
        public List<ResultadoLeyesSignosOperaciones> Resultados { get; set; } = new();
        public int CantidadEjercicios { get; set; }
        public int CantidadOperandos { get; set; }
        public TipoEjercicioLeyes TipoEjercicio { get; set; }
        public string VelocidadEjercicio { get; set; } = "0";
        public int MinDigitos { get; set; }
        public int MaxDigitos { get; set; }

    }
}
