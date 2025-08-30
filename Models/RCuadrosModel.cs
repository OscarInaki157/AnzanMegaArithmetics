namespace AnzanMegaArithmetics.Models
{
    public class RCuadrosModel
    {
        public int RespuestaUsuario { get; set; }
        public int RespuestaCorrecta { get; set; }
        public bool Respondido { get; set; }
        public double TiempoRespuesta { get; set; }
        public bool EsCorrecto => Respondido && RespuestaUsuario == RespuestaCorrecta; 
    }
}
