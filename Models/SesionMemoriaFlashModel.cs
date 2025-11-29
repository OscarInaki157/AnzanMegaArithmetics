namespace AnzanMegaArithmetics.Models
{
    public class SesionMemoriaFlashModel
    {
        public ConfMemoriaFlashModel Configuracion { get; set; }
        public List<EjercicioMemoriaFlashModel> Ejercicios { get; set; }
    }
}
