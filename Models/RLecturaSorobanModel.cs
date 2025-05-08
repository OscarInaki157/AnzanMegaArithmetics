namespace AnzanMegaArithmetics.Models
{
    public class RLecturaSorobanModel
    {
        public long RespuestaUsuario { get; set; }
        public long RespuestaCorrecta { get; set; }
        public bool EsCorrecto => RespuestaUsuario == RespuestaCorrecta;
    }
}
