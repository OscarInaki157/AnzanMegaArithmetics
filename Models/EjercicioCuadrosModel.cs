namespace AnzanMegaArithmetics.Models
{
    public class EjercicioCuadrosModel
    {
        //lista de los numeros de cada cuadro
        public int NumeroEjercicio { get; set; }

        public List<int> Rejilla { get; set; }

        public string Iluminacion { get; set; } = string.Empty;

        public int RCorrecta => Sumatoria();

        private int Sumatoria()
        {
            return Rejilla.Sum();
        }
    }
}
