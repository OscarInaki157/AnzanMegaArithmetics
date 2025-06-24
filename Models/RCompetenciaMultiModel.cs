namespace AnzanMegaArithmetics.Models
{
    public class RCompetenciaMultiModel
    {
        public string OperacionTexto { get; set; }
        public long RespuestaCorrecta { get; set; }
        public long RespuestaUsuario { get; set; }
        public bool Respondido { get; set; }
        public bool EsCorrecto { get; set; }
        public TimeSpan TiempoRespuesta { get; set; }
    }
}
