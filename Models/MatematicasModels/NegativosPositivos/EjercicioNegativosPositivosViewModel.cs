namespace AnzanMegaArithmetics.Models.MatematicasModels.NegativosPositivos
{
    public class EjercicioNegativosPositivosViewModel
    {
        // ── Configuración ────────────────────────────────────
        public int CantidadEjercicios { get; set; }
        public int MinDigitos { get; set; }
        public int MaxDigitos { get; set; }
        public string TipoEjercicio { get; set; } = "clasico";
        public string VelocidadEjercicio { get; set; } = "0";
        public string TipoDigitos { get; set; } = "ambos";

        // ── Progreso ─────────────────────────────────────────
        public int EjerciciosRealizados { get; set; }
        public string ResultadosJson { get; set; } = "[]";

        // ── Ejercicio actual ──────────────────────────────────
        public int Operando1 { get; set; }
        public int Operando2 { get; set; }
        public string Operacion { get; set; } = "+";
        public int Resultado { get; set; }
        public PosicionIncognita Posicion { get; set; } = PosicionIncognita.Resultado;
        public int RespuestaCorrecta { get; set; }

        // ── Respuesta del usuario ─────────────────────────────
        public int RespuestaUsuario { get; set; } = int.MinValue;
        public double TiempoRespuesta { get; set; }

        // ── Tutorial ──────────────────────────────────────────
        public bool EsTutorial { get; set; } = false;
        public int PasoTutorial { get; set; } = 0;

        // ── Computed ──────────────────────────────────────────
        public bool UsarRecta => MinDigitos == 1 && MaxDigitos == 1;

    }
}
