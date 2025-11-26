using AnzanMegaArithmetics.Models;

namespace AnzanMegaArithmetics.Services
{
    public interface IUsersDBService
    {
        public LoginResponseModel ValidateLogin(string user, string pass);
        public LoginResponseModel ObtenerUserDashboard(int id_Usuario);
        public bool UltimaConexion(LoginResponseModel user);
        public List<RankingUsersModel> ObtenerRankingUsuarios(int cantidad = 0);
        public List<RankingSlideModel> ObtenerRankingsSlider(int cantidad = 10);
        public int ListarUsersTotales();
        public ListUsersModel ObtenerUsuarios();
        public string ActualizarUser(ActualizarUsuarioModel actualizar);
        public string ActualizarClasesUsuario(List<int> clases, int Id_Usuario);
        public string ActualizarLicenciaUsuario(string nueva, int Id_Usuario);
        public string CrearNuevoUsuario(ActualizarUsuarioModel nuevo);
        public string EliminarUsuario(ActualizarUsuarioModel model);
    }
}
