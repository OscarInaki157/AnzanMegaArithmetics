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

        public int ListarClasesTotales() 
        {
            int contador = 0;
            try
            {
                contador = _context.Clases.Count();
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

        public string ActualizarUser(ActualizarUsuarioModel actualizar)
        {
            if (actualizar == null || actualizar.Id_Usuario <= 0 || actualizar.ClasesSeleccionadas == null)
            {
                return "Error datos de usuario inválidos";
            }

            try
            {
                UsuariosDB usuarioDB = _context.Usuarios.FirstOrDefault(x => x.Id_Usuario == actualizar.Id_Usuario);
                if (usuarioDB == null)
                {
                    return "Error al actualizar usuario";
                }

                // Actualizar los campos del usuario
                usuarioDB.Id_Rol = actualizar.Id_Rol;
                usuarioDB.Nombre = actualizar.Nombre;
                usuarioDB.Correo = actualizar.Correo;
                usuarioDB.Gamer_Tag = actualizar.Gamer_Tag;
                usuarioDB.Pass = actualizar.Pass;
                usuarioDB.Activo = actualizar.Activo;
                usuarioDB.Racha = actualizar.Racha;
                usuarioDB.Experiencia_Total = actualizar.Exp;

                //actualizar clases
                List<int> clases = actualizar.ClasesSeleccionadas;
                string actualizo =  ActualizarClasesUsuario(clases, actualizar.Id_Usuario);
                if (actualizo.StartsWith("Error")) 
                {
                    return actualizo;
                }

                //actualizar licencia
                string actualizoLicencia = ActualizarLicenciaUsuario(actualizar.Licencia, actualizar.Id_Usuario);
                if (actualizoLicencia.StartsWith("Error"))
                {
                    return actualizoLicencia;
                }

                _context.SaveChanges();
                return "Usuario actualizado correctamente";

            }
            catch (Exception ex)
            {
                return "Error al actualizar el usuario: " + ex.Message;
            }
        }

        public string ActualizarClasesUsuario(List<int> clases, int Id_Usuario) 
        {
            try 
            {
                var clasesActuales = _context.Usuarios_Clases.Where(uc => uc.Id_Usuario == Id_Usuario);
                _context.Usuarios_Clases.RemoveRange(clasesActuales);

                foreach (var Id_clase in clases)
                {
                    _context.Usuarios_Clases.Add(new Usuario_ClaseDB
                    {
                        Id_Usuario = Id_Usuario,
                        Id_Clase = Id_clase
                    });
                }
                _context.SaveChanges();
                return "Clases del usuario actualizadas correctamente";
            }
            catch (Exception ex) 
            {
                return "Error al actualizar las clases del usuario: " + ex.Message;
            }
            
        }

        public string ActualizarLicenciaUsuario(string nueva, int Id_Usuario)
        {
            try
            {
                //obtener id de la licencia
                int LicenciaId = _context.Licencias.Where(l => l.Nombre == nueva)
                    .Select(l => l.Id_Licencia)
                    .FirstOrDefault();
                if (LicenciaId == 0) 
                {
                    return "Error: Licencia no encontrada";
                }

                //modificar la licencia del usuario
                //licencia actual
                var usuarioLicencia = _context.Usuarios_Licencias.FirstOrDefault(ul => ul.Id_Usuario == Id_Usuario);

                //obtener vigencia
                var busqueda = _context.Licencias.FirstOrDefault(l => l.Id_Licencia == LicenciaId);
                int vigencia = busqueda.Vigencia;
                if (vigencia <= 0) 
                {
                    vigencia = 12; //por defecto un año
                }

                if (usuarioLicencia == null)
                {
                    usuarioLicencia = new Usuarios_LicenciasDB
                    {
                        Id_Usuario = Id_Usuario,
                        Id_Licencia = LicenciaId,
                        Fecha_Asignacion = DateTime.Now,
                        Fecha_Vencimiento = DateTime.Now.AddMonths(vigencia),
                        Vigencia = _context.Licencias
                            .Where(l => l.Id_Licencia == LicenciaId)
                            .Select(l => l.Vigencia)
                            .FirstOrDefault()
                    };
                }
                else 
                {
                    usuarioLicencia.Id_Licencia = LicenciaId;
                    usuarioLicencia.Fecha_Asignacion = DateTime.Now;
                    usuarioLicencia.Fecha_Vencimiento = DateTime.Now.AddMonths(vigencia);
                    usuarioLicencia.Vigencia = vigencia;
                }

                _context.SaveChanges();
                return "Licencia del usuario actualizada correctamente";
            }
            catch (Exception ex)
            {
                return "Error al actualizar la licencia del usuario: " + ex.Message;
            }
        }

        public string CrearNuevoUsuario(ActualizarUsuarioModel nuevo)
        {
            if (nuevo == null || nuevo.ClasesSeleccionadas == null)
            {
                return "Error datos de usuario inválidos";
            }

            //excepcion, buscar si ya existe un usuario con el mismo gamer tag o correo
            var usuarioExistente = _context.Usuarios.FirstOrDefault(u => u.Gamer_Tag == nuevo.Gamer_Tag || u.Correo == nuevo.Correo);
            if (usuarioExistente != null)
            {
                return "Error: Ya existe un usuario con el mismo Gamer Tag o Correo.";
            }

            try
            {
                //Insertar nuevo usuario
                UsuariosDB usuariosDB = new UsuariosDB
                {
                    Id_Rol = nuevo.Id_Rol,
                    Nombre = nuevo.Nombre,
                    Correo = nuevo.Correo,
                    Gamer_Tag = nuevo.Gamer_Tag,
                    Pass = nuevo.Pass,
                    Activo = nuevo.Activo,
                    Racha = nuevo.Racha,
                    Experiencia_Total = nuevo.Exp,
                    Ultima_Actividad = DateTime.Now
                };

                _context.Usuarios.Add(usuariosDB);
                _context.SaveChanges();
                //Insertar clases_usuario

                int Id_Usuario = _context.Usuarios.Where(u => u.Gamer_Tag == nuevo.Gamer_Tag && u.Correo == nuevo.Correo && u.Pass == nuevo.Pass)
                  .Select(u => u.Id_Usuario)
                  .FirstOrDefault();

                foreach (var Id_clase in nuevo.ClasesSeleccionadas)
                {
                    _context.Usuarios_Clases.Add(new Usuario_ClaseDB
                    {
                        Id_Usuario = Id_Usuario,
                        Id_Clase = Id_clase
                    });
                }
                _context.SaveChanges();

                //Insertar licencia_usuario
              
                int Id_Licencia = _context.Licencias.Where(l => l.Nombre == nuevo.Licencia)
                    .Select(l => l.Id_Licencia)
                    .FirstOrDefault();
                int vigencia = _context.Licencias.Where(l => l.Nombre == nuevo.Licencia).Select(l => l.Vigencia).FirstOrDefault();
                Usuarios_LicenciasDB usuario_licencia = new Usuarios_LicenciasDB
                {
                    Id_Usuario = Id_Usuario,
                    Id_Licencia = Id_Licencia,
                    Fecha_Asignacion = DateTime.Now,
                    Fecha_Vencimiento = DateTime.Now.AddMonths(vigencia),
                    Vigencia = vigencia
                };

                _context.Usuarios_Licencias.Add(usuario_licencia);
                _context.SaveChanges();

                return "Exito creando el usuario";
            }
            catch (Exception ex)
            {
                return "Error al crear el nuevo usuario: " + ex.Message;
            }
        }

        public string EliminarUsuario(ActualizarUsuarioModel model) 
        {
            int Id_Usuario = model.Id_Usuario;

            try 
            {
                var usuarioDB = _context.Usuarios.FirstOrDefault(u => u.Id_Usuario == Id_Usuario);
                if (usuarioDB == null) 
                {
                    return "Error: Usuario no encontrado";
                }
                //eliminar pruebas del wey
                var pruebasUsuario = _context.Pruebas.Where(p => p.Id_Usuario == Id_Usuario);
                _context.Pruebas.RemoveRange(pruebasUsuario);
                //eliminar clases asociadas
                var clasesUsuario = _context.Usuarios_Clases.Where(uc => uc.Id_Usuario == Id_Usuario);
                _context.Usuarios_Clases.RemoveRange(clasesUsuario);
                //eliminar licencias asociadas
                var licenciasUsuario = _context.Usuarios_Licencias.Where(ul => ul.Id_Usuario == Id_Usuario);
                _context.Usuarios_Licencias.RemoveRange(licenciasUsuario);
                //eliminar usuario
                _context.Usuarios.Remove(usuarioDB);
                _context.SaveChanges();
                return "Usuario eliminado correctamente";
            }
            catch (Exception ex) 
            {
                return "Error al eliminar el usuario: " + ex.Message;
            }
        }
    }
}
