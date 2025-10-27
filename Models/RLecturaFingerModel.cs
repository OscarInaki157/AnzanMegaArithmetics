namespace AnzanMegaArithmetics.Models
{
    public class RLecturaFingerModel
    {
        public int RespuestaUsuario { get; set; }
        public int RespuestaCorrecta { get; set; }
        public double TiempoRespuesta { get; set; }
        public bool EsCorrecto => RespuestaUsuario == RespuestaCorrecta;
    }
}
