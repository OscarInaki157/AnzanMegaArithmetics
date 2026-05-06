namespace AnzanMegaArithmetics.Models.MatematicasModels.LeyesSignosOperaciones
{
    public class ResultadoLeyesSignosOperaciones
    {
        public TipoEjercicioLeyes TipoEjercicio { get; set; }
        public int CantidadOperandos { get; set; }
        public double RespuestaCorrecta { get; set; }
        public int CoefRespuesta { get; set; }
        public string RespuestaUsuario { get; set; } = "";
        public double TiempoRespuesta { get; set; }
        public string Enunciado { get; set; } = "";
        public string Variable { get; set; } = "x";

        public bool EsCorrecto
        {
            get
            {
                if (EsOmitido) return false;
                if (TipoEjercicio == TipoEjercicioLeyes.PuroSigno)
                    return (RespuestaUsuario == "+") == (RespuestaCorrecta >= 0);
                if (TipoEjercicio == TipoEjercicioLeyes.Literales)
                    return int.TryParse(RespuestaUsuario, out int v) && v == CoefRespuesta;
                if (double.TryParse(RespuestaUsuario,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double d))
                    return Math.Abs(d - RespuestaCorrecta) < 0.01;
                return false;
            }
        }
        public bool EsOmitido =>
            string.IsNullOrEmpty(RespuestaUsuario) || RespuestaUsuario == "omitido";
    }

}
