using Microsoft.IdentityModel.Tokens;

namespace AnzanMegaArithmetics.Models
{
    public class RNumeroFigura
    {
        public int NumeroEjercicio { get; set; }
        public string TipoPreguntaEjercicio { get; set; }
        public List<int> ElementosPregunta { get; set; }
        public List<int> RespuestaCorrecta { get; set; }
        public List<int> RespuestaUsuario { get; set; }
        public bool Respondido => RespuestaUsuario != null;
        public bool EsCorrecto => Respondido && RespuestaUsuario.SequenceEqual(RespuestaCorrecta);
        public double TiempoRespuesta { get; set; }

    }
}
