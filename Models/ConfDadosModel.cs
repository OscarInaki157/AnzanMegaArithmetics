namespace AnzanMegaArithmetics.Models
{
    public class ConfDadosModel
    {
        public int CantidadEjercicios { get; set; }
        public int TiempoTotal { get; set; }
        public int TiempoMeditacion { get; set; }
        public string TipoPrueba { get; set; } = string.Empty;
    }
}
