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
                // Normalizamos el input: quitamos espacios y pasamos a minúsculas
                string userNormalized = user.Replace(" ", "").ToLower();

                UsuariosDB userDB = _context.Usuarios.FirstOrDefault(x =>
                    (x.Usuario.Replace(" ", "").ToLower() == userNormalized ||
                     x.Nombre.Replace(" ", "").ToLower() == userNormalized) &&
                    x.Pass == pass);

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
