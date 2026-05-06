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
        public TipoEjercicioLeyes TipoEjercicioConfig { get; set; }
        public TipoEjercicioLeyes TipoEjercicioActual { get; set; }
        public string VelocidadEjercicio { get; set; } = "0";

        // ── Progreso ─────────────────────────────────────────
        public int EjerciciosRealizados { get; set; }
        public string ResultadosJson { get; set; } = "[]";

        // ── Operandos múltiples (serializado como JSON) ──────
        // Lista de signos: true=positivo, false=negativo
        public string SignosJson { get; set; } = "[]";
        // Lista de valores absolutos
        public string ValoresJson { get; set; } = "[]";
        // Lista de operaciones ENTRE operandos (N-1 para N operandos)
        public string OperacionesJson { get; set; } = "[]";
        // Lista de coeficientes (para literales)
        public string CoefsJson { get; set; } = "[]";
        public string Variable { get; set; } = "x";

        // ── Campos específicos por tipo ───────────────────────

        // Exponente: base y exponente (1 solo operando)
        public bool SignoA { get; set; } = true;
        public double ValorA { get; set; }
        public int Exponente { get; set; } = 2;

        // Raíz: valor bajo la raíz (siempre positivo para √)
        public double ValorRaiz { get; set; }

        // Paréntesis/Corchetes: estructura jerárquica serializada
        // Formato JSON: { "grupos": [ {signos, valores, ops}, ... ], "opsEntre": [...] }
        public string EstructuraJson { get; set; } = "{}";

        // Respuesta
        public double RespuestaCorrecta { get; set; }
        public int CoefRespuesta { get; set; }  // para literales

        // ── Respuesta del usuario ─────────────────────────────
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
