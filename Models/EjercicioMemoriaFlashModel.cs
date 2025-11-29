namespace AnzanMegaArithmetics.Models
{
    public class EjercicioMemoriaFlashModel
    {
        public int Id_Ejercicio { get; set; }
        public int EjercicioNumero { get; set; }
        public List<DigitoEjercicioMFModel> Digitos { get; set; }
        public string ColorClase { get; set; }
        public string ParejaMemoriaRuta { get; set; }

    }
}
