namespace AnzanMegaArithmetics.Models
{
    public class RSumaRestaModel
    {
        public int EjercicioNumero { get; set; }             
        public List<string> OperacionesMostradas { get; set; }
        public int RespuestaCorrecta { get; set; }
        public int RespuestaUsuario { get; set; }
        public bool EsCorrecto => RespuestaUsuario == RespuestaCorrecta;
        public bool Respondido { get; set; }
    }
}
