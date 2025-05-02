namespace AnzanMegaArithmetics.Models
{
    public class REscrituraSorobanModel
    {
        public ulong RespuestaUsuario { get; set; }
        public ulong RespuestaCorrecta { get; set; }
        public bool EsCorrecto => RespuestaUsuario == RespuestaCorrecta;
    }
}
