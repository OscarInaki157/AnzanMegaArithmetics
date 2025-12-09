using AnzanMegaArithmetics.Helpers;
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

        public LoginResponseModel ObtenerUserDashboard(int id_Usuario)
        {
       
            LoginResponseModel response = new();
            try
            {
                response = _context.Usuarios
                    .Include(u => u.Rol)
                    .Include(u => u.Usuario_Clase)
                    .ThenInclude(uc => uc.Clase)
                    .Include(u => u.UsuarioLicencias)
                    .ThenInclude(ul => ul.Licencia)
                    .Where(u => u.Id_Usuario == id_Usuario)
                    .Select(u => new LoginResponseModel
                    {
                        Id_Usuario = u.Id_Usuario,
                        Nombre = u.Nombre,
                        Correo = u.Correo,
                        Gamer_Tag = u.Gamer_Tag,
                        Id_Rol = u.Rol.Rol,
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
                            .FirstOrDefault() ?? "Sin licencia"
                    })
                    .FirstOrDefault();
                if (response == null) 
                {
                    return new LoginResponseModel { Id_Usuario = 0 };
                }
                return response;
            }
            catch (Exception ex)
            {
                return new LoginResponseModel { Id_Usuario = 0 };
            }
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

        public List<RankingUsersModel> ObtenerRankingUsuarios(int cantidad = 0)
        {
            List<RankingUsersModel> ranking = new List<RankingUsersModel>();
            try
            {
                var query = _context.Usuarios
                    .Include(u => u.Usuario_Clase)
                    .ThenInclude(uc => uc.Clase)
                    .OrderByDescending(u => u.Experiencia_Total)
                    .AsQueryable();

                if (cantidad > 0)
                {
                    query = query.Take(cantidad);
                }

                ranking = query.Select(u => new RankingUsersModel
                {
                    Id_Usuario = u.Id_Usuario,
                    Nombre = u.Nombre,
                    Correo = u.Correo,
                    Gamer_Tag = u.Gamer_Tag,
                    Clases = u.Usuario_Clase
                        .Where(uc => uc.Clase != null)
                        .Select(uc => uc.Clase.Nombre)
                        .ToList(),
                    Exp = u.Experiencia_Total
                }).ToList();
            }
            catch (Exception ex)
            {
                return new List<RankingUsersModel>();
            }
            return ranking;
        }

        public List<RankingSlideModel> ObtenerRankingsSlider(int cantidad = 10)
        {
            var slides = new List<RankingSlideModel>();

            var ordenDeseado = new Dictionary<string, int>
            {
                { "🌍 Ranking Global", 1 },
                { "Fingermath Lectura", 2 },
                { "Fingermath Escritura", 3 },
                { "Soroban Lectura", 4 },
                { "Soroban Escritura", 5 },
                { "Suma Resta", 6 },
                { "Números Flash", 7 },
                { "Dictado Flash", 8},
                { "Tablas de Multiplicar", 9 },
                { "Multiplicación", 10 },
                { "Número Figura", 11 },
                { "CalendarioMental - ", 12 },
                { "Calendario Mental - Competencia", 13 },
                { "Cuadros Práctica", 14 },
                { "Potencias - ", 15 },
                { "Potencias - Competencia", 16 },
                { "Raíces - ", 17 },
                { "Raíces - Competencia", 18 },
            };

            const int ORDEN_POR_DEFECTO = 100;

            try
            {
                var globalUsers = _context.Usuarios
                    .Include(u => u.Usuario_Clase).ThenInclude(uc => uc.Clase)
                    .OrderByDescending(u => u.Experiencia_Total)
                    .Take(cantidad > 0 ? cantidad : 100)
                    .Select(u => new RankingUsersModel
                    {
                        Id_Usuario = u.Id_Usuario,
                        Nombre = u.Nombre,
                        Correo = u.Correo,
                        Gamer_Tag = u.Gamer_Tag,
                        Exp = u.Experiencia_Total,
                        // Si necesitas las clases:
                        Clases = u.Usuario_Clase.Where(uc => uc.Clase != null).Select(uc => uc.Clase.Nombre).ToList()
                    })
                    .ToList();

                var globalTitulo = "🌍 Ranking Global";
                slides.Add(new RankingSlideModel
                {
                    Titulo = globalTitulo,
                    Datos = globalUsers, 
                    Orden = ordenDeseado.GetValueOrDefault(globalTitulo, ORDEN_POR_DEFECTO)
                });

                var actividades = _context.Pruebas
                    .Where(p => p.Activo)
                    .GroupBy(p => p.Tipo_Prueba)
                    .Select(grupo => new
                    {
                        NombreActividad = grupo.Key,
                        TopUsuarios = grupo.GroupBy(p => p.Id_Usuario)
                                           .Select(gUser => new RankingUsersModel
                                           {
                                               Id_Usuario = gUser.Key,
                                               Nombre = gUser.FirstOrDefault().Usuario.Nombre,
                                               Gamer_Tag = gUser.FirstOrDefault().Usuario.Gamer_Tag,
                                               Exp = gUser.Sum(x => x.ExperienciaAdquirida)
                                           })
                                           .OrderByDescending(u => u.Exp)
                                           .Take(cantidad > 0 ? cantidad : 100)
                                           .ToList()
                    })
                    .ToList();

                foreach (var act in actividades)
                {
                    int orden = ordenDeseado.GetValueOrDefault(act.NombreActividad, ORDEN_POR_DEFECTO);

                    slides.Add(new RankingSlideModel
                    {
                        Titulo = "📊 " + act.NombreActividad,
                        Datos = act.TopUsuarios,
                        Orden = orden
                    });
                }
                slides = slides.OrderBy(s => s.Orden).ToList();
            }
            catch (Exception ex)
            {
                // Manejo de errores (loguear si es necesario)
                Console.WriteLine(ex.Message);
            }

            return slides;
        }


        public RankingSlideModel ObtenerRankingFiltrado(string periodo, string actividad, int cantidad = 10) 
        {
            DateTime fechaInicio;
            string tituloPeriodo;

            if (periodo.Equals("Semanal", StringComparison.OrdinalIgnoreCase))
            {
                fechaInicio = DateTime.Today.AddDays(-7);
                tituloPeriodo = "Semanal";
            }
            else if (periodo.Equals("Mensual", StringComparison.OrdinalIgnoreCase))
            {
                fechaInicio = DateTime.Today.AddDays(-30);
                tituloPeriodo = "Mensual";
            }
            else
            {
                return new RankingSlideModel { Titulo = "Periodo no válido", Datos = new List<RankingUsersModel>() };
            }

            var baseQuery = _context.Pruebas.Where(p => p.Activo && p.Fecha >= fechaInicio);

            if (!actividad.Equals("Ranking Global", StringComparison.OrdinalIgnoreCase))
            {
                baseQuery = baseQuery.Where(p => p.Tipo_Prueba.Equals(actividad));
            }

            var topUsuariosFiltrado = baseQuery
            .GroupBy(p => p.Id_Usuario)
            .Select(gUser => new RankingUsersModel
            {
                Id_Usuario = gUser.Key,
                Nombre = gUser.FirstOrDefault().Usuario.Nombre,
                Gamer_Tag = gUser.FirstOrDefault().Usuario.Gamer_Tag,
                Exp = gUser.Sum(x => x.ExperienciaAdquirida)
            })
            .OrderByDescending(u => u.Exp)
            .Take(cantidad)
            .ToList();

                string prefijo = actividad.Equals("Ranking Global", StringComparison.OrdinalIgnoreCase) ? "🌍" : "📊";
                string tituloFinal = actividad.Equals("Ranking Global", StringComparison.OrdinalIgnoreCase) ? "Ranking Global" : actividad;


                return new RankingSlideModel
                {
                    Titulo = $"{prefijo} {tituloFinal} ({tituloPeriodo})",
                    Datos = topUsuariosFiltrado
                };

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


        //retos


        public int ClaimChallengeReward(int userId, int retoId)
        {
            var retoACrear = GetMasterChallenges().FirstOrDefault(r => r.IdReto == retoId);
            if (retoACrear == null) return 0;

            var today = DateTime.Today.Date;

            var usuario = _context.Usuarios.FirstOrDefault(u => u.Id_Usuario == userId);
            if (usuario == null) return 0; 

            List<int> reclamadosHoy = new List<int>();

            if (usuario.Fecha_Ultimo_Reclamo.Date == today)
            {
                reclamadosHoy = usuario.Retos_Reclamados_Hoy
                    .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(int.Parse)
                    .ToList();
            }
            else
            {

                usuario.Retos_Reclamados_Hoy = string.Empty;
            }

            if (reclamadosHoy.Contains(retoId))
            {
                return 0; 
            }

            var userActivitiesToday = _context.Pruebas
                .Where(a => a.Id_Usuario == userId && a.Fecha >= today)
                .ToList();

            bool isCompleted = ValidateChallengeCompletion(retoACrear, userActivitiesToday);

            if (isCompleted)
            {
               
                usuario.Experiencia_Total += retoACrear.RecompensaXP;

                reclamadosHoy.Add(retoId);
                usuario.Retos_Reclamados_Hoy = string.Join(",", reclamadosHoy);
                usuario.Fecha_Ultimo_Reclamo = DateTime.Now;

                _context.SaveChanges();

                return retoACrear.RecompensaXP;
            }

            return 0;
        }

        public List<DailyChallengeViewModel> GetUserDailyChallenges(int userId)
        {
            var masterChallenges = GetMasterChallenges();
            var today = DateTime.Today.Date;

            // OBTENER ESTADO DEL USUARIO DESDE LA DB
            var usuario = _context.Usuarios.FirstOrDefault(u => u.Id_Usuario == userId);
            if (usuario == null) return new List<DailyChallengeViewModel>();

            List<int> reclamadosHoy = new List<int>();
            if (usuario.Fecha_Ultimo_Reclamo.Date == today)
            {
                reclamadosHoy = usuario.Retos_Reclamados_Hoy
                    .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(int.Parse)
                    .ToList();
            }

            int seed = today.DayOfYear + today.Year;
            var random = new Random(seed);
            var dailyChallenges = masterChallenges.OrderBy(x => random.Next()).Take(4).ToList();

            var userActivitiesToday = _context.Pruebas
                .Where(a => a.Id_Usuario == userId && a.Fecha >= today)
                .ToList();

            foreach (var reto in dailyChallenges)
            {
                reto.EsCompletado = ValidateChallengeCompletion(reto, userActivitiesToday);
                reto.FueReclamado = reclamadosHoy.Contains(reto.IdReto);
            }

            return dailyChallenges;
        }

        public List<DailyChallengeViewModel> GetMasterChallenges()
        {
            return new List<DailyChallengeViewModel>
            {
                // === 1. RETOS DE EFECTIVIDAD (Utilizan Efectividad100, 90, 80) ===
                //fingermath
                new DailyChallengeViewModel {
                    IdReto = 1, Descripcion = "Obtén 100% de efectividad en cualquier prueba de Fingermath.",
                    RecompensaXP = 100, TipoActividad = TipoActividadEnum.Efectividad100, ValorObjetivo = 100
                },
                new DailyChallengeViewModel {
                    IdReto = 2, Descripcion = "Obtén al menos 90% de efectividad en cualquier prueba de Fingermath.",
                    RecompensaXP = 90, TipoActividad = TipoActividadEnum.Efectividad90, ValorObjetivo = 90
                },
                new DailyChallengeViewModel {
                    IdReto = 3, Descripcion = "Obtén al menos 80% de efectividad en cualquier prueba de Fingermath.",
                    RecompensaXP = 80, TipoActividad = TipoActividadEnum.Efectividad80, ValorObjetivo = 80
                },
                //soroban
                new DailyChallengeViewModel {
                    IdReto = 4, Descripcion = "Obtén 100% de efectividad en cualquier prueba de Soroban.",
                    RecompensaXP = 100, TipoActividad = TipoActividadEnum.Efectividad100, ValorObjetivo = 100
                },
                new DailyChallengeViewModel {
                    IdReto = 5, Descripcion = "Obtén al menos 90% de efectividad en cualquier prueba de Soroban.",
                    RecompensaXP = 90, TipoActividad = TipoActividadEnum.Efectividad90, ValorObjetivo = 90
                },
                new DailyChallengeViewModel {
                    IdReto = 6, Descripcion = "Obtén al menos 80% de efectividad en cualquier prueba de Soroban.",
                    RecompensaXP = 80, TipoActividad = TipoActividadEnum.Efectividad80, ValorObjetivo = 80
                },
                //suma resta
                new DailyChallengeViewModel {
                    IdReto = 7, Descripcion = "Obtén 100% de efectividad en una sesión de Suma y Resta.",
                    RecompensaXP = 100, TipoActividad = TipoActividadEnum.Efectividad100, ValorObjetivo = 100
                },
                new DailyChallengeViewModel {
                    IdReto = 8, Descripcion = "Obtén al menos 90% de efectividad en una sesión de Suma y Resta.",
                    RecompensaXP = 90, TipoActividad = TipoActividadEnum.Efectividad90, ValorObjetivo = 90
                },
                new DailyChallengeViewModel {
                    IdReto = 9, Descripcion = "Obtén al menos 80% de efectividad en en una sesión de Suma y Resta.",
                    RecompensaXP = 80, TipoActividad = TipoActividadEnum.Efectividad80, ValorObjetivo = 80
                },
                //numeros flash
                new DailyChallengeViewModel {
                    IdReto = 10, Descripcion = "Obtén 100% de efectividad en una sesión de Números Flash.",
                    RecompensaXP = 100, TipoActividad = TipoActividadEnum.Efectividad100, ValorObjetivo = 100
                },
                new DailyChallengeViewModel {
                    IdReto = 11, Descripcion = "Obtén al menos 90% de efectividad en una sesión de Números Flash.",
                    RecompensaXP = 90, TipoActividad = TipoActividadEnum.Efectividad90, ValorObjetivo = 90
                },
                new DailyChallengeViewModel {
                    IdReto = 12, Descripcion = "Obtén al menos 80% de efectividad en una sesión de Números Flash.",
                    RecompensaXP = 80, TipoActividad = TipoActividadEnum.Efectividad80, ValorObjetivo = 80
                },
                //tablas de multiplicar
                new DailyChallengeViewModel {
                    IdReto = 13, Descripcion = "Obtén 100% de efectividad en una prueba de Tablas de Multiplicar.",
                    RecompensaXP = 100, TipoActividad = TipoActividadEnum.Efectividad100, ValorObjetivo = 100
                },
                new DailyChallengeViewModel {
                    IdReto = 14, Descripcion = "Obtén al menos 90% de efectividad en prueba de Tablas de Multiplicar.",
                    RecompensaXP = 90, TipoActividad = TipoActividadEnum.Efectividad90, ValorObjetivo = 90
                },
                new DailyChallengeViewModel {
                    IdReto = 15, Descripcion = "Obtén al menos 80% de efectividad prueba de Tablas de Multiplicar.",
                    RecompensaXP = 80, TipoActividad = TipoActividadEnum.Efectividad80, ValorObjetivo = 80
                },
                //multiplicacion
                new DailyChallengeViewModel {
                    IdReto = 16, Descripcion = "Obtén 100% de efectividad en una prueba de Multiplicación.",
                    RecompensaXP = 100, TipoActividad = TipoActividadEnum.Efectividad100, ValorObjetivo = 100
                },
                new DailyChallengeViewModel {
                    IdReto = 17, Descripcion = "Obtén al menos 90% de efectividad en prueba de Multiplicación.",
                    RecompensaXP = 90, TipoActividad = TipoActividadEnum.Efectividad90, ValorObjetivo = 90
                },
                new DailyChallengeViewModel {
                    IdReto = 18, Descripcion = "Obtén al menos 80% de efectividad prueba de Multiplicación.",
                    RecompensaXP = 80, TipoActividad = TipoActividadEnum.Efectividad80, ValorObjetivo = 80
                },
                //calendario mental
                 new DailyChallengeViewModel {
                    IdReto = 19, Descripcion = "Obtén 100% de efectividad en una prueba de Calendario Mental.",
                    RecompensaXP = 100, TipoActividad = TipoActividadEnum.Efectividad100, ValorObjetivo = 100
                },
                new DailyChallengeViewModel {
                    IdReto = 20, Descripcion = "Obtén al menos 90% de efectividad en prueba de Calendario Mental.",
                    RecompensaXP = 90, TipoActividad = TipoActividadEnum.Efectividad90, ValorObjetivo = 90
                },
                new DailyChallengeViewModel {
                    IdReto = 21, Descripcion = "Obtén al menos 80% de efectividad prueba de Calendario Mental.",
                    RecompensaXP = 80, TipoActividad = TipoActividadEnum.Efectividad80, ValorObjetivo = 80
                },
                //raices
                 new DailyChallengeViewModel {
                    IdReto = 22, Descripcion = "Obtén 100% de efectividad en una prueba de Raices cuadradas.",
                    RecompensaXP = 100, TipoActividad = TipoActividadEnum.Efectividad100, ValorObjetivo = 100
                },
                new DailyChallengeViewModel {
                    IdReto = 23, Descripcion = "Obtén al menos 90% de efectividad en prueba de Raices cuadradas.",
                    RecompensaXP = 90, TipoActividad = TipoActividadEnum.Efectividad90, ValorObjetivo = 90
                },
                new DailyChallengeViewModel {
                    IdReto = 24, Descripcion = "Obtén al menos 80% de efectividad prueba de Raices cuadradas.",
                    RecompensaXP = 80, TipoActividad = TipoActividadEnum.Efectividad80, ValorObjetivo = 80
                },
               //potencias
               new DailyChallengeViewModel {
                    IdReto = 25, Descripcion = "Obtén 100% de efectividad en una prueba de Números al cuadrado.",
                    RecompensaXP = 100, TipoActividad = TipoActividadEnum.Efectividad100, ValorObjetivo = 100
                },
                new DailyChallengeViewModel {
                    IdReto = 26, Descripcion = "Obtén al menos 90% de efectividad en prueba de Números al cuadrado",
                    RecompensaXP = 90, TipoActividad = TipoActividadEnum.Efectividad90, ValorObjetivo = 90
                },
                new DailyChallengeViewModel {
                    IdReto = 27, Descripcion = "Obtén al menos 80% de efectividad prueba de Números al cuadrado",
                    RecompensaXP = 80, TipoActividad = TipoActividadEnum.Efectividad80, ValorObjetivo = 80
                },

                // === 2. RETOS DE CONTEO DE PRUEBAS COMPLETADAS (Utilizan CompletarPruebas...) ===
                new DailyChallengeViewModel {
                    IdReto = 28, Descripcion = "Completa 3 pruebas de Fingermath.",
                    RecompensaXP = 100, TipoActividad = TipoActividadEnum.CompletarPruebasFingermath, ValorObjetivo = 3
                },
                new DailyChallengeViewModel {
                    IdReto = 29, Descripcion = "Completa 3 pruebas de Soroban.",
                    RecompensaXP = 100, TipoActividad = TipoActividadEnum.CompletarPruebasSoroban, ValorObjetivo = 3
                },
                new DailyChallengeViewModel {
                    IdReto = 30, Descripcion = "Completa 3 pruebas de Suma y Resta.",
                    RecompensaXP = 100, TipoActividad = TipoActividadEnum.CompletarPruebasSumaResta, ValorObjetivo = 3
                },
                new DailyChallengeViewModel {
                    IdReto = 31, Descripcion = "Completa 3 pruebas de Números Flash.",
                    RecompensaXP = 100, TipoActividad = TipoActividadEnum.CompletarPruebasFlash, ValorObjetivo = 3
                },
                new DailyChallengeViewModel {
                    IdReto = 32, Descripcion = "Completa 3 pruebas de Tablas de Multiplicar.",
                    RecompensaXP = 100, TipoActividad = TipoActividadEnum.CompletarPruebasTablasMultiplicar, ValorObjetivo = 3
                },
                new DailyChallengeViewModel {
                    IdReto = 33, Descripcion = "Completa 3 pruebas de Multiplicación.",
                    RecompensaXP = 100, TipoActividad = TipoActividadEnum.CompletarPruebasMultiplicacion, ValorObjetivo = 3
                },
                new DailyChallengeViewModel {
                    IdReto = 34, Descripcion = "Completa 1 prueba de Competencia Multiplicación 3x3.",
                    RecompensaXP = 100, TipoActividad = TipoActividadEnum.CompletarPruebasCompetenciaMultiplicacion3x3, ValorObjetivo = 1
                },
                new DailyChallengeViewModel {
                    IdReto = 35, Descripcion = "Completa 1 prueba de Número Figura.",
                    RecompensaXP = 100, TipoActividad = TipoActividadEnum.CompletarPruebasNumeroFigura, ValorObjetivo = 1
                },
                new DailyChallengeViewModel {
                    IdReto = 36, Descripcion = "Completa 1 prueba de Cuadros de Velocidad en modo Práctica.",
                    RecompensaXP = 100, TipoActividad = TipoActividadEnum.CompletarPruebasCuadrosPractica, ValorObjetivo = 1
                },
                new DailyChallengeViewModel {
                    IdReto = 37, Descripcion = "Completa 1 prueba de Calendario Mental.",
                    RecompensaXP = 100, TipoActividad = TipoActividadEnum.CompletarPruebasCalendarioMental, ValorObjetivo = 1
                },
                new DailyChallengeViewModel {
                    IdReto = 38, Descripcion = "Completa 1 prueba de Calendario Mental en modo Competencia.",
                    RecompensaXP = 100, TipoActividad = TipoActividadEnum.CompletarPruebasCalendarioMentalCompetencia, ValorObjetivo = 1
                },
                new DailyChallengeViewModel {
                    IdReto = 39, Descripcion = "Completa 2 pruebas de Números al cuadrado.",
                    RecompensaXP = 100, TipoActividad = TipoActividadEnum.CompletarPruebasPotencias, ValorObjetivo = 2
                },
                new DailyChallengeViewModel {
                    IdReto = 40, Descripcion = "Completa 2 pruebas de Raices cuadradas.",
                    RecompensaXP = 100, TipoActividad = TipoActividadEnum.CompletarPruebasRaices, ValorObjetivo = 2
                },
                new DailyChallengeViewModel {
                    IdReto = 41, Descripcion = "Completa 1 prueba de Matemáticas con Dados.",
                    RecompensaXP = 100, TipoActividad = TipoActividadEnum.CompletarPruebasMatematicasconDados, ValorObjetivo = 1
                },
                // === 3. RETOS DE ACUMULACIÓN (Utilizan AcumularXP, AcumularPreguntasCorrectas) ===
                new DailyChallengeViewModel {
                    IdReto = 42, Descripcion = "Gana 500 Puntos de Experiencia (XP) hoy.",
                    RecompensaXP = 150, TipoActividad = TipoActividadEnum.AcumularXP, ValorObjetivo = 500
                },
                new DailyChallengeViewModel {
                    IdReto = 43, Descripcion = "Gana 700 Puntos de Experiencia (XP) hoy.",
                    RecompensaXP = 200, TipoActividad = TipoActividadEnum.AcumularXP, ValorObjetivo = 700
                },
                new DailyChallengeViewModel {
                    IdReto = 44, Descripcion = "Gana 800 Puntos de Experiencia (XP) hoy.",
                    RecompensaXP = 250, TipoActividad = TipoActividadEnum.AcumularXP, ValorObjetivo = 800
                },
                new DailyChallengeViewModel {
                    IdReto = 45, Descripcion = "Gana 900 Puntos de Experiencia (XP) hoy.",
                    RecompensaXP = 300, TipoActividad = TipoActividadEnum.AcumularXP, ValorObjetivo = 900
                },
                new DailyChallengeViewModel {
                    IdReto = 46, Descripcion = "Gana 1000 Puntos de Experiencia (XP) hoy.",
                    RecompensaXP = 500, TipoActividad = TipoActividadEnum.AcumularXP, ValorObjetivo = 1000
                },
                new DailyChallengeViewModel {
                    IdReto = 47, Descripcion = "Responde correctamente un total de 50 preguntas hoy.",
                    RecompensaXP = 200, TipoActividad = TipoActividadEnum.AcumularPreguntasCorrectas, ValorObjetivo = 50
                },
                new DailyChallengeViewModel {
                    IdReto = 48, Descripcion = "Responde correctamente un total de 100 preguntas hoy.",
                    RecompensaXP = 300, TipoActividad = TipoActividadEnum.AcumularPreguntasCorrectas, ValorObjetivo = 100
                },
                new DailyChallengeViewModel {
                    IdReto = 49, Descripcion = "Responde correctamente un total de 10 preguntas hoy.",
                    RecompensaXP = 100, TipoActividad = TipoActividadEnum.AcumularPreguntasCorrectas, ValorObjetivo = 10
                }

                  //aqui poner más retos
        
            };
        }

        public string GetDBStringForCountOrTime(TipoActividadEnum tipo)
        {
            switch (tipo)
            {
                case TipoActividadEnum.CompletarPruebasFingermath:
                case TipoActividadEnum.TiempoPruebasFingermath:
                    return "Fingermath";

                case TipoActividadEnum.CompletarPruebasSoroban:
                case TipoActividadEnum.TiempoPruebasSoroban:
                    return "Soroban";

                case TipoActividadEnum.CompletarPruebasSumaResta:
                case TipoActividadEnum.TiempoPruebasSumaResta:
                    return "Suma Resta";

                case TipoActividadEnum.CompletarPruebasFlash:
                case TipoActividadEnum.TiempoPruebasFlash:
                    return "Números Flash";

                case TipoActividadEnum.CompletarPruebasTablasMultiplicar:
                case TipoActividadEnum.TiempoPruebasTablasMultiplicar:
                    return "Tablas de Multiplicar";

                case TipoActividadEnum.CompletarPruebasMultiplicacion:
                case TipoActividadEnum.TiempoPruebasMultiplicacion:
                    return "Multiplicación";

                case TipoActividadEnum.CompletarPruebasCompetenciaMultiplicacion3x3:
                case TipoActividadEnum.TiempoPruebasCompetenciaMultiplicacion3x3:
                    return "Competencia Multiplicación - 3x3";

                case TipoActividadEnum.CompletarPruebasNumeroFigura:
                case TipoActividadEnum.TiempoPruebasNumeroFigura:
                    return "Número Figura";

                case TipoActividadEnum.CompletarPruebasCuadrosPractica:
                case TipoActividadEnum.TiempoPruebasCuadrosPractica:
                    return "CuadrosPractica";

                case TipoActividadEnum.CompletarPruebasCalendarioMental:
                case TipoActividadEnum.TiempoPruebasCalendarioMental:
                case TipoActividadEnum.CompletarPruebasCalendarioMentalCompetencia:
                case TipoActividadEnum.TiempoPruebasCalendarioMentalCompetencia:
                    return "Calendario Mental -";

                case TipoActividadEnum.CompletarPruebasRaices:
                case TipoActividadEnum.TiempoPruebasRaices:
                    return "Raices -";

                case TipoActividadEnum.CompletarPruebasPotencias:
                case TipoActividadEnum.TiempoPruebasPotencias:
                    return "Potencias -";

                case TipoActividadEnum.CompletarPruebasMatematicasconDados:
                case TipoActividadEnum.TiempoPruebasMatematicasconDados:
                    return "Matemáticas con Dados";

                default:
                    
                    return string.Empty;
            }
        }

        public bool ValidateChallengeCompletion(DailyChallengeViewModel reto, List<DataBase.PruebasDB> activities)
        {
            // Filtrar solo las pruebas que se consideran completas o activas
            var finalActivities = activities.Where(a => a.Activo == true).ToList();
            var objetivo = reto.ValorObjetivo.GetValueOrDefault();

            // Obtener la clave base para búsquedas
            string tipoPruebaDB = GetDBStringForCountOrTime(reto.TipoActividad);

            switch (reto.TipoActividad)
            {
                // ===========================================
                // GRUPO 1: EFECTIVIDAD / ACIERTO (1, 2, 3)
                // Regla: Encontrar al menos UNA prueba que cumpla el % de efectividad
                // ===========================================
                case TipoActividadEnum.Efectividad100:
                case TipoActividadEnum.Efectividad90:
                case TipoActividadEnum.Efectividad80:
                    return finalActivities.Any(a =>
                        // Usamos StartsWith() para incluir "Fingermath Lectura", etc.
                        a.Tipo_Prueba.StartsWith(tipoPruebaDB) &&
                        a.Total_Preguntas > 0 &&
                        // Cálculo de efectividad
                        ((double)a.Respuestas_Correctas / a.Total_Preguntas * 100) >= objetivo);


                // ===========================================
                // GRUPO 2: CONTEO DE PRUEBAS (4 - 17)
                // Regla: Contar cuántas pruebas del tipo base hay
                // ===========================================
                case TipoActividadEnum.CompletarPruebasFingermath:
                case TipoActividadEnum.CompletarPruebasSoroban:
                case TipoActividadEnum.CompletarPruebasSumaResta:
                case TipoActividadEnum.CompletarPruebasFlash:
                case TipoActividadEnum.CompletarPruebasTablasMultiplicar:
                case TipoActividadEnum.CompletarPruebasMultiplicacion:
                case TipoActividadEnum.CompletarPruebasCompetenciaMultiplicacion3x3:
                case TipoActividadEnum.CompletarPruebasNumeroFigura:
                case TipoActividadEnum.CompletarPruebasCuadrosPractica:
                case TipoActividadEnum.CompletarPruebasCalendarioMental:
                case TipoActividadEnum.CompletarPruebasCalendarioMentalCompetencia:
                case TipoActividadEnum.CompletarPruebasRaices:
                case TipoActividadEnum.CompletarPruebasPotencias:
                case TipoActividadEnum.CompletarPruebasMatematicasconDados:
                    // Contar el número de pruebas que comienzan con la clave base.
                    return finalActivities.Count(a =>
                        a.Tipo_Prueba.StartsWith(tipoPruebaDB)) >= objetivo;


                // ===========================================
                // GRUPO 3: TIEMPO ACUMULADO (18 - 31)
                // Regla: Sumar el tiempo total de las pruebas del tipo base
                // ===========================================
                case TipoActividadEnum.TiempoPruebasFingermath:
                case TipoActividadEnum.TiempoPruebasSoroban:
                case TipoActividadEnum.TiempoPruebasSumaResta:
                case TipoActividadEnum.TiempoPruebasFlash:
                case TipoActividadEnum.TiempoPruebasTablasMultiplicar:
                case TipoActividadEnum.TiempoPruebasMultiplicacion:
                case TipoActividadEnum.TiempoPruebasCompetenciaMultiplicacion3x3:
                case TipoActividadEnum.TiempoPruebasNumeroFigura:
                case TipoActividadEnum.TiempoPruebasCuadrosPractica:
                case TipoActividadEnum.TiempoPruebasCalendarioMental:
                case TipoActividadEnum.TiempoPruebasCalendarioMentalCompetencia:
                case TipoActividadEnum.TiempoPruebasRaices:
                case TipoActividadEnum.TiempoPruebasPotencias:
                case TipoActividadEnum.TiempoPruebasMatematicasconDados:
                    // Sumar los minutos de todas las pruebas que comienzan con la clave base.
                    var totalTime = finalActivities
                        .Where(a => a.Tipo_Prueba.StartsWith(tipoPruebaDB))
                        .Sum(a => a.Tiempo.TotalMinutes);

                    return totalTime >= objetivo;


                // ===========================================
                // GRUPO 4: VARIOS / ACUMULACIÓN GLOBAL (32, 33)
                // Regla: Suma global de una columna específica
                // ===========================================
                case TipoActividadEnum.AcumularXP:
                    // Sumar la ExperienciaAdquirida de todas las pruebas de hoy.
                    var totalXP = finalActivities.Sum(a => a.ExperienciaAdquirida);
                    return totalXP >= objetivo;

                case TipoActividadEnum.AcumularPreguntasCorrectas:
                    // Sumar las Respuestas_Correctas de todas las pruebas de hoy.
                    var totalRespuestas = finalActivities.Sum(a => a.Respuestas_Correctas);
                    return totalRespuestas >= objetivo;

                default:
                    return false;
            }
        }



    }
}
