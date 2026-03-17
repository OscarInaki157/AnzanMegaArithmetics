namespace AnzanMegaArithmetics.Models
{
    public class ResultadoRMViewModel
    {
        public int TotalEjercicios { get; set; }
        public int Aciertos { get; set; }
        public int Errores { get; set; }
        public double Porcentaje { get; set; }
        public double TiempoTotal { get; set; }
        public double TiempoPromedio { get; set; }
        public int ExperienciaGanada { get; set; }
        public ConfRutasModel Configuracion { get; set; }
        public List<RRutaMModel> DetallesEjercicios { get; set; } = new List<RRutaMModel>();
    }
}