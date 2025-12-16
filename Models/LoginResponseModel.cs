using System.ComponentModel.DataAnnotations;

namespace AnzanMegaArithmetics.Models
{
    public class LoginResponseModel
    {
        public int Id_Usuario { get; set; }
        public string Id_Rol {  get; set; }
        public string Nombre { get; set; }
        public string Correo { get; set; }
        public string Gamer_Tag { get; set; }
        public List<string> Clases { get; set; } = new List<string>();

        //campos para la vista
        public int Racha { get; set; }
        public int Exp {  get; set; }
        public string Rango_Actual { get; set; }
        public DateTime Ultima_Cnx { get; set; }
        public string Licencia { get; set; }
        public string Frase_Bienvenida { get; set; } = string.Empty;
    }
}
