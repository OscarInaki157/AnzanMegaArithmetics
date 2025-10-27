namespace AnzanMegaArithmetics.Models
{
    public class REscrituraSorobanModel
    {
        public long RespuestaUsuario { get; set; }
        public long RespuestaCorrecta { get; set; }
        public double TiempoRespuesta { get; set; }
        public bool EsCorrecto => RespuestaUsuario != -1 && RespuestaUsuario == RespuestaCorrecta;
    }
}
