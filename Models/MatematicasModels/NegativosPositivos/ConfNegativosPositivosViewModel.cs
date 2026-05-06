namespace AnzanMegaArithmetics.Models.MatematicasModels.NegativosPositivos
{
    public class ConfNegativosPositivosViewModel
    {
        public int CantidadEjercicios { get; set; } = 10;
        public int MinDigitos { get; set; } = 1;
        public int MaxDigitos { get; set; } = 1;
        // "clasico"   → siempre A op B = ?
        // "aleatorio" → mezcla dónde va el ?
        public string TipoEjercicio { get; set; } = "clasico";
        // "0" = libre, o segundos como string ("5", "10", etc.)
        public string VelocidadEjercicio { get; set; } = "0";
        // "superiores" (6-9) | "inferiores" (1-5) | "ambos"
        public string TipoDigitos { get; set; } = "ambos";
    }
}
