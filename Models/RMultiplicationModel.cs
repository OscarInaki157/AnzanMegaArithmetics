namespace AnzanMegaArithmetics.Models
{
    public class RMultiplicationModel
    {
        public int Multiplicando { get; set; }
        public int Multiplicador { get; set; }
        public int RespuestaUsuario { get; set; } = -1;
        public int RespuestaCorrecta => Multiplicando * Multiplicador;
        public bool EsCorrecto => RespuestaUsuario == RespuestaCorrecta;
        public bool Respondido => RespuestaUsuario != -1;
        public double TiempoRespuesta { get; set; }

        public string OperacionTexto => $"{Multiplicando} × {Multiplicador}";
    }
}
