using AnzanMegaArithmetics.Models;
using DataBase;
using Microsoft.EntityFrameworkCore;

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
                string userNormalized = user.Replace(" ", "").ToLower();

                var userDB = _context.Usuarios
                    .Include(u => u.Rol)  // Para obtener el rol
                    .Include(u => u.Usuario_Clase)  // Para obtener las clases
                    .ThenInclude(uc => uc.Clase)  // Para obtener los datos de la clase
                    .Include(u => u.UsuarioLicencias)
                    .ThenInclude(ul => ul.Licencia)
                .FirstOrDefault(x =>
                    (x.Correo.Replace(" ", "").ToLower() == userNormalized ||
                     x.Gamer_Tag.Replace(" ", "").ToLower() == userNormalized ||
                     x.Nombre.Replace(" ", "").ToLower() == userNormalized) &&
                     x.Pass == pass && x.Activo == true);

                if (userDB != null)
                {
                    response.Id_Usuario = userDB.Id_Usuario;
                    response.Nombre = userDB.Nombre;
                    response.Correo = userDB.Correo;
                    response.Gamer_Tag = userDB.Gamer_Tag;

                    response.Id_Rol = userDB.Rol?.Rol ?? "Alumno";

                    response.Clases = userDB.Usuario_Clase
                    .Where(uc => uc.Clase != null) // Filtrar clases nulas
                    .Select(uc => uc.Clase.Nombre) // Obtener solo los nombres
                    .ToList();

                    var licenciaActiva = userDB.UsuarioLicencias
                       .Where(ul => ul.Fecha_Vencimiento >= DateTime.Now &&
                                   ul.Licencia != null)  // Solo licencias vigentes y con datos
                       .OrderByDescending(ul => ul.Fecha_Vencimiento)  // La más reciente
                       .FirstOrDefault();


                    if (licenciaActiva != null)
                    {
                        response.Licencia = licenciaActiva.Licencia.Nombre;

                    }
                    else
                    {
                        // Verificar si tiene licencias pero están vencidas
                        var licenciaVencida = userDB.UsuarioLicencias
                            .Where(ul => ul.Licencia != null)
                            .OrderByDescending(ul => ul.Fecha_Vencimiento)
                            .FirstOrDefault();

                        if (licenciaVencida != null)
                        {
                            response.Licencia = $"{licenciaVencida.Licencia.Nombre} (Vencida)";
                        }
                        else
                        {
                            response.Licencia = "Sin licencia";
                        }
                    }


                    response.Racha = userDB.Racha;
                    response.Exp = userDB.Experiencia_Total;
                    response.Ultima_Cnx = userDB.Ultima_Actividad;

                }
                else
                {
                    response.Gamer_Tag = "No hay coincidencias";
                }
            }
            catch (Exception ex)
            {
                response.Gamer_Tag = "Error al validar el inicio de sesión: " + ex.Message;
            }

            return response;
        }

        public bool UltimaConexion(LoginResponseModel user)
        {
            try
            {
                UsuariosDB act2 = _context.Usuarios.Find(user.Id_Usuario);
                if (act2 != null) 
                {
                    act2.Ultima_Actividad = DateTime.Now;
                    _context.SaveChanges();
                    return true;
                }
                return false;
            }
            catch (Exception ex) 
            {
                return false;
            }
        }

        //CRUD de usuarios para el panel de administración

        public int ListarUsersTotales()
        {
            int contador = 0;
            try
            {
                contador = _context.Usuarios.Count();
                return contador;
            }
            catch (Exception ex)
            {
                return contador;
            }
        }
        public ListUsersModel ObtenerUsuarios() 
        {
            ListUsersModel model = new ListUsersModel();

            try 
            {
                //recuperar todas las clases en lista
                model.Clases = _context.Clases
                    .Select(c => new ClaseBDModel 
                    {
                        Id_Clase = c.Id_Clase,
                        Nombre = c.Nombre
                    }).ToList();

                //recuperar todas las licencias en lista
                model.Licencias = _context.Licencias
                    .Select(l => new LicenciaBDModel 
                    {
                        Id_Licencia = l.Id_Licencia,
                        Nombre = l.Nombre,
                        Vigencia = l.Vigencia
                    }).ToList();

                //recuperar todos los roles en lista
                model.Roles = _context.Roles
                    .Select(r => new RolBDModel 
                    {
                        Id_Rol = r.Id_Rol,
                        Rol = r.Rol,
                        Nivel = r.Nivel
                    }).ToList();

                //recuperar todos los usuarios finales en lista
                model.UsuariosFinales = _context.Usuarios
                    .Include(u => u.Rol)
                    .Include(u => u.Usuario_Clase)
                    .ThenInclude(uc => uc.Clase)
                    .Include(u => u.UsuarioLicencias)
                    .ThenInclude(ul => ul.Licencia)
                    .Select(u => new UsuarioBDModel 
                    {
                        Id_Usuario = u.Id_Usuario,
                        Id_Rol = u.Rol.Rol,
                        Nombre = u.Nombre,
                        Correo = u.Correo,
                        Gamer_Tag = u.Gamer_Tag,
                        Pass = u.Pass,
                        Activo = u.Activo,
                        Clases = u.Usuario_Clase
                            .Where(uc => uc.Clase != null)
                            .Select(uc => uc.Clase.Nombre)
                            .ToList(),
                        Racha = u.Racha,
                        Exp = u.Experiencia_Total,
                        Ultima_Cnx = u.Ultima_Actividad,

                        Licencia = u.UsuarioLicencias
                            .Where(ul => ul.Licencia != null)
                            .Select(ul => ul.Licencia.Nombre)
                            .FirstOrDefault() ?? "Sin licencia",
                        Fecha_Asignacion_Licencia = u.UsuarioLicencias
                            .Where(ul => ul.Licencia != null)
                            .Select(ul => ul.Fecha_Asignacion)
                            .FirstOrDefault(),
                        Fecha_Vencimiento_Licencia = u.UsuarioLicencias
                            .Where(ul => ul.Licencia != null)
                            .Select(ul => ul.Fecha_Vencimiento)
                            .FirstOrDefault()
                    }).ToList();
            }
            catch (Exception ex) 
            {
                return new ListUsersModel();
            }

            return model;
        }

    }
}
