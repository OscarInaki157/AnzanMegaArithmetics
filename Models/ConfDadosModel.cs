namespace AnzanMegaArithmetics.Models
{
    public class ConfDadosModel
    {
        public int CantidadEjercicios { get; set; }
        public int TiempoTotal { get; set; }
        public int TiempoMeditacion { get; set; }
        public string TipoPrueba { get; set; } = string.Empty;
        public string ModoJuego { get; set; } = "Aritmetica";
        public bool UsaJerarquia { get; set; } = true;
        public string RangoDados { get; set; }
        public int MultiplicadorLibre { get; set; }
        public int NumeroDados { get; set; }
    }
}
