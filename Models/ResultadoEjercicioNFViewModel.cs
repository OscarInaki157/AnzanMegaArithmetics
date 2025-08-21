namespace AnzanMegaArithmetics.Models
{
    public class ResultadoEjercicioNFViewModel
    {
        public int NumeroEjercicio { get; set; }
        public List<int> ElementosPregunta { get; set; }
        public List<int> RespuestaCorrecta { get; set; }
        public List<int> RespuestaUsuario { get; set; }
        public double TiempoRespuesta { get; set; }
        public bool EsCorrecto { get; set; }

        public string RespuestaUsuarioStr =>
            RespuestaUsuario[0] == -1 ? "No respondió" : string.Join(", ", RespuestaUsuario);

        public string RespuestaCorrectaStr => string.Join(", ", RespuestaCorrecta);
    }
}
