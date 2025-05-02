namespace AnzanMegaArithmetics.Models
{
    public class RLecturaSorobanModel
    {
        public ulong RespuestaUsuario { get; set; }
        public ulong RespuestaCorrecta { get; set; }
        public bool EsCorrecto => RespuestaUsuario == RespuestaCorrecta;
    }
}
