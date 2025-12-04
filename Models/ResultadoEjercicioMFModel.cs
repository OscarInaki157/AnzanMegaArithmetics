namespace AnzanMegaArithmetics.Models
{
    public class ResultadoEjercicioMFModel : EjercicioMemoriaFlashModel
    {
        public int RespuestaUsuario { get; set; }
        public bool EsCorrecto { get; set; }
        public bool Respondido { get; set; }
    }
}
