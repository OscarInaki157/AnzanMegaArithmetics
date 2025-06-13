namespace AnzanMegaArithmetics.Models
{
    public class RSumaRestaModel
    {
        public string OperacionTexto { get; set; }
        public long RespuestaCorrecta { get; set; }
        public long RespuestaUsuario { get; set; }
        public bool EsCorrecto => RespuestaUsuario == RespuestaCorrecta;
        public bool Respondido { get; set; }
        public double TiempoRespuesta {  get; set; }
    }
}
