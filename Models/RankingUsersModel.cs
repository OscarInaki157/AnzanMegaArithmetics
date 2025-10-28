namespace AnzanMegaArithmetics.Models
{
    public class RankingUsersModel
    {
        public int Id_Usuario { get; set; }
        public string Nombre { get; set; }
        public string Correo { get; set; }
        public string Gamer_Tag { get; set; }
        public List<string> Clases { get; set; } = new List<string>(); //lista de clases del usuario
        public int Exp { get; set; }
    }
}
