namespace AnzanMegaArithmetics.Models
{
    public class EjercicioDadosModel
    {
        public int Id_Ejercicio { get; set; }
        public int[] Dados { get; set; } = new int[5];
        public int Dado_Resultado { get; set; }
        public List<string> SolucionesPosibles { get; set; } = new();
    }
}
