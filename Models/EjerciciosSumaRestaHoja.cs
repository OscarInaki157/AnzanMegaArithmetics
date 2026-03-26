namespace AnzanMegaArithmetics.Models
{
    public class EjerciciosSumaRestaHoja
    {
        public List<long> Numeros { get; set; } = new List<long>();
        public List<string> Operaciones { get; set; } = new List<string>();
        public long RespuestaCorrecta { get; set; }
    }
}
