using AnzanMegaArithmetics.Models;

namespace AnzanMegaArithmetics.Services
{
    public interface IUsersDBService
    {
        public LoginResponseModel ValidateLogin(string user, string pass);
        public bool UltimaConexion(LoginResponseModel user);
        public int ListarUsersTotales();
        public ListUsersModel ObtenerUsuarios();
        public string ActualizarUser(ActualizarUsuarioModel actualizar);
        public string ActualizarClasesUsuario(List<int> clases, int Id_Usuario);
        public string ActualizarLicenciaUsuario(string nueva, int Id_Usuario);
        public string CrearNuevoUsuario(ActualizarUsuarioModel nuevo);
        public string EliminarUsuario(ActualizarUsuarioModel model);
    }
}
