namespace AnzanMegaArithmetics.Models
{
    public class RMultiplicacionModel
    {
        public int Ejercicio { get; set; }

        public int Multiplicando { get; set; }
        public int Multiplicador { get; set; }

        public int RespuestaUsuario { get; set; }

        public int RespuestaCorrecta => Multiplicando * Multiplicador;
        public bool EsCorrecto => RespuestaUsuario == RespuestaCorrecta;
        public bool Respondido => RespuestaUsuario != -1;
        public double TiempoRespuesta { get; set; } // Tiempo en segundos

        public string OperacionTexto => $"{Multiplicando} × {Multiplicador}";

    }
}
