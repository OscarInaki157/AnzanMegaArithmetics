namespace AnzanMegaArithmetics.Models
{
    public class ListUsersModel
    {
        public List<ClaseBDModel> Clases { get; set; }
        public List<LicenciaBDModel> Licencias { get; set; }
        public List<RolBDModel> Roles { get; set; }

        public List<UsuarioBDModel> UsuariosFinales { get; set; }
    }
}
