namespace AnzanMegaArithmetics.Models.MatematicasModels.NegativosPositivos
{
    // Enum compartido — define dónde va el incógnito
    public enum PosicionIncognita
    {
        Resultado,
        Operando1,
        Operando2
    }
    public class ResultadoNegativosPositivos
    {
        public int Operando1 { get; set; }
        public int Operando2 { get; set; }
        public string Operacion { get; set; } = "+";
        public int Resultado { get; set; }
        public PosicionIncognita Posicion { get; set; }
        public int RespuestaCorrecta { get; set; }
        public int RespuestaUsuario { get; set; }
        public double TiempoRespuesta { get; set; }
        public bool EsCorrecto => RespuestaUsuario == RespuestaCorrecta;

    }
}
