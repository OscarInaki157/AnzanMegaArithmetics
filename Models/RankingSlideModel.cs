namespace AnzanMegaArithmetics.Models
{
    public class RankingSlideModel
    {
        public string Titulo { get; set; }
        public List<RankingUsersModel> Datos { get; set; }
        public int Orden { get; set; }
    }
}
