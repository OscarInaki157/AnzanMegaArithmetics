namespace AnzanMegaArithmetics.Models
{
    public class RLecturaSorobanModel
    {
        public long RespuestaUsuario { get; set; }
        public long RespuestaCorrecta { get; set; }
        public double TiempoRespuesta { get; set; }
        public bool EsCorrecto => RespuestaUsuario == RespuestaCorrecta;
    }
}
