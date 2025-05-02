namespace AnzanMegaArithmetics.Models
{
    public class RFlashModel
    {
        public string OperacionTexto { get; set; } = "";
        public int RespuestaCorrecta { get; set; }
        public int RespuestaUsuario { get; set; }
        public bool Respondido { get; set; }
        public bool EsCorrecto => Respondido && RespuestaUsuario == RespuestaCorrecta;
    }


}
