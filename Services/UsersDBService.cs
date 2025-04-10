using AnzanMegaArithmetics.Models;
using DataBase;

namespace AnzanMegaArithmetics.Services
{
    public class UsersDBService : IUsersDBService
    {
        private readonly AnzanMegaContext _context;
        public UsersDBService(AnzanMegaContext context)
        {
            this._context = context;
        }

        public LoginResponseModel ValidateLogin(string user, string pass)
        {
            LoginResponseModel response = new LoginResponseModel();
            try
            {
                UsuariosDB userDB = _context.Usuarios.FirstOrDefault(x =>
    (x.Usuario.ToLower().Equals(user.ToLower()) || x.Nombre.ToLower().Equals(user.ToLower())) && x.Pass.Equals(pass));

                if (userDB != null)
                {
                    response.Id_Usuario = userDB.Id_Usuario;
                    response.Nombre = userDB.Nombre;
                    response.Clase = userDB.Clase;
                    response.Usuario = userDB.Usuario;
                    response.Rol = userDB.Rol;
                }
                else
                {
                    response.Usuario = "No hay coincidencias";
                }
            }
            catch (Exception ex)
            {
                response.Usuario = "Error al validar el inicio de sesión: " + ex.Message;
            }

            return response;
        }

    }
}
