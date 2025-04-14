namespace AnzanMegaArithmetics.Models
{
    public class REscrituraSorobanModel
    {
        public int RespuestaUsuario { get; set; }
        public int RespuestaCorrecta { get; set; }
        public bool EsCorrecto => RespuestaUsuario == RespuestaCorrecta;
    }
}
