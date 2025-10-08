using AnzanMegaArithmetics.Models;

namespace AnzanMegaArithmetics.Services
{
    public interface IUsersDBService
    {
        public LoginResponseModel ValidateLogin(string user, string pass);

        public bool UltimaConexion(LoginResponseModel user);
    }
}
