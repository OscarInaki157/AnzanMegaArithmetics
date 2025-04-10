namespace AnzanMegaArithmetics.Models
{
    public class RLecturaFingerModel
    {
        public int RespuestaUsuario { get; set; }
        public int RespuestaCorrecta { get; set; }
        public bool EsCorrecto => RespuestaUsuario == RespuestaCorrecta;
    }
}
