namespace AnzanMegaArithmetics.Models.MatematicasModels.LeyesSignosOperaciones
{
    public enum OperacionLeyes { Suma, Resta, Multiplicacion, Division, Potencia, Raiz }

    public class EjercicioLeyesSignosOperacionesViewModel
    {
        // ── Configuración ────────────────────────────────────
        public int CantidadEjercicios { get; set; }
        public int CantidadOperandos { get; set; } = 2;
        public int MinDigitos { get; set; }
        public int MaxDigitos { get; set; }
        // Tipos y ops serializados para persistir entre ejercicios
        public string TiposSeleccionadosJson { get; set; } = "[0]";
        // Suma y Resta van juntas como una sola opción
        public bool OpSumaResta { get; set; } = true;
        public bool OpMultiplicacion { get; set; } = true;
        public bool OpDivision { get; set; } = true;

        // Tipo real del ejercicio actual (ya resuelto el sorteo)
        public TipoEjercicioLeyes TipoEjercicioActual { get; set; }
        public string VelocidadEjercicio { get; set; } = "0";

        // ── Progreso ─────────────────────────────────────────
        public int EjerciciosRealizados { get; set; }
        public string ResultadosJson { get; set; } = "[]";

        // ── Operandos múltiples ───────────────────────────────
        public string SignosJson { get; set; } = "[]";
        public string ValoresJson { get; set; } = "[]";
        public string OperacionesJson { get; set; } = "[]";
        public string CoefsJson { get; set; } = "[]";
        public string Variable { get; set; } = "x";

        // ── Campos específicos por tipo ───────────────────────
        public bool SignoA { get; set; } = true;
        public double ValorA { get; set; }
        public int Exponente { get; set; } = 2;
        public bool ExpConParentesis { get; set; } = true; // punto 7
        public double ValorRaiz { get; set; }
        public string EstructuraJson { get; set; } = "{}";

        // ── Respuesta ─────────────────────────────────────────
        public double RespuestaCorrecta { get; set; }
        public int CoefRespuesta { get; set; }
        public string RespuestaUsuario { get; set; } = "";
        public double TiempoRespuesta { get; set; }

        // ── Tutorial ─────────────────────────────────────────
        public bool EsTutorial { get; set; } = false;
        public int PasoTutorial { get; set; } = 0;

        // ── Helpers ──────────────────────────────────────────
        public bool UsaDecimales => TipoEjercicioActual == TipoEjercicioLeyes.Reales;
        public bool EsPuroSigno => TipoEjercicioActual == TipoEjercicioLeyes.PuroSigno;
        public bool EsLiteral => TipoEjercicioActual == TipoEjercicioLeyes.Literales;
        public bool EsRaiz => TipoEjercicioActual == TipoEjercicioLeyes.Raiz;
        public bool EsExponente => TipoEjercicioActual == TipoEjercicioLeyes.Exponente;
    }

}
