namespace AnzanMegaArithmetics.Models
{
    public class REscrituraSorobanModel
    {
        public long RespuestaUsuario { get; set; }
        public long RespuestaCorrecta { get; set; }
        public bool EsCorrecto => RespuestaUsuario == RespuestaCorrecta;
    }
}
