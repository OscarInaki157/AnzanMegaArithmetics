namespace AnzanMegaArithmetics.Models
{
    public class RDivisionModel
    {
        public int Dividendo { get; set; }
        public int Divisor { get; set; }
        public double RespuestaUsuario { get; set; } = -1;
        public double RespuestaCorrecta => Math.Round((double)Dividendo / Divisor, 3);
        public bool EsCorrecto => Respondido && Math.Abs(RespuestaUsuario - RespuestaCorrecta) < 0.001;
        public bool Respondido => RespuestaUsuario != -1;
        public double TiempoRespuesta { get; set; }

        public string OperacionTexto => $"{Dividendo} ÷ {Divisor}";
    }
}
