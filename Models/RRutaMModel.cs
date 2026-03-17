namespace AnzanMegaArithmetics.Models
{
    public class RRutaMModel
    {
        public int Id_Ejercicio { get; set; }
        public OERutaMModel ElementoPregunta { get; set; }
        public List<OERutaMModel> ListaOpciones { get; set; } = new List<OERutaMModel>();

        //respuesta
        public List<OERutaMModel> RespuestaSeleccionada { get; set; } = new List<OERutaMModel>();
        public double TiempoRespuesta { get; set; }
        public bool EsCorrecta { get; set; }
    }
}
