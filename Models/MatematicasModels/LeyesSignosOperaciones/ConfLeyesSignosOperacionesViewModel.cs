namespace AnzanMegaArithmetics.Models.MatematicasModels.LeyesSignosOperaciones
{
    public enum TipoEjercicioLeyes
    {
        PuroSigno = 0,
        Literales = 1,
        Reales = 2,
        Parentesis = 3,
        Exponente = 4,
        Aleatorio = 5,
        Corchetes = 6,
        Raiz = 7
    }

    public class ConfLeyesSignosOperacionesViewModel
    {
        public int CantidadEjercicios { get; set; } = 10;
        public int CantidadOperandos { get; set; } = 2;
        public int MinDigitos { get; set; } = 1;
        public int MaxDigitos { get; set; } = 1;
        public string VelocidadEjercicio { get; set; } = "0";

        public List<int> TiposSeleccionados { get; set; } = new() { 0 }; 
        public bool OpSumaResta { get; set; } = true;
        public bool OpMultiplicacion { get; set; } = true;
        public bool OpDivision { get; set; } = true;
    }

}
