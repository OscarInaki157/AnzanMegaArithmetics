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
                    string rangoCalculado = CalcularRango(userDB.Experiencia_Total);
                    
                    if (string.IsNullOrEmpty(userDB.Rango_Actual) || userDB.Rango_Actual != rangoCalculado)
                    {
                        userDB.Rango_Actual = rangoCalculado;
                        _context.SaveChanges();
                    }

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
                    response.Rango_Actual = userDB.Rango_Actual;

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
                        Rango_Actual = u.Rango_Actual,
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

        private List<int> GetRolesExcluidosIds()
        {
            return _context.Roles
                .Where(r => r.Rol.Contains("Admin") || r.Rol == "Master" || r.Rol == "Profesor")
                .Select(r => r.Id_Rol)
                .ToList();
        }

        public List<RankingUsersModel> ObtenerRankingUsuarios(int cantidad = 0)
        {
            List<RankingUsersModel> ranking = new List<RankingUsersModel>();
            try
            {
                var rolesExcluidos = GetRolesExcluidosIds();

                var query = _context.Usuarios
                    .Where(u => !rolesExcluidos.Contains(u.Id_Rol))
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
                    Exp = u.Experiencia_Total,
                    Racha = u.Racha,
                    Rango_Actual = u.Rango_Actual
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
                { "🔥 Racha de Días", 2 },
                { "Fingermath Lectura", 3 },
                { "Fingermath Escritura", 4 },
                { "Soroban Lectura", 5 },
                { "Soroban Escritura", 6 },
                { "Suma Resta", 7 },
                { "Números Flash", 8 },
                { "Dictado Flash", 9},
                { "Tablas de Multiplicar", 10 },
                { "Multiplicación", 11 },
                { "Número Figura", 12 },
                { "CalendarioMental - ", 13 },
                { "Calendario Mental - Competencia", 14 },
                { "Cuadros Práctica", 15 },
                { "Potencias - ", 16 },
                { "Potencias - Competencia", 17 },
                { "Raíces - ", 18 },
                { "Raíces - Competencia", 19 },
            };

            const int ORDEN_POR_DEFECTO = 100;

            try
            {
                var rolesExcluidos = GetRolesExcluidosIds();

                var globalUsers = _context.Usuarios
                    .Where(u => !rolesExcluidos.Contains(u.Id_Rol))
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
                        Racha = u.Racha,
                        Clases = u.Usuario_Clase.Where(uc => uc.Clase != null).Select(uc => uc.Clase.Nombre).ToList(),
                        Rango_Actual = u.Rango_Actual
                    })
                    .ToList();

                var globalTitulo = "🌍 Ranking Global";
                slides.Add(new RankingSlideModel
                {
                    Titulo = globalTitulo,
                    Datos = globalUsers, 
                    Orden = ordenDeseado.GetValueOrDefault(globalTitulo, ORDEN_POR_DEFECTO)
                });

                var rachaUsers = _context.Usuarios
                    .Where(u => !rolesExcluidos.Contains(u.Id_Rol))
                    .OrderByDescending(u => u.Racha)
                    .Take(cantidad > 0 ? cantidad : 100)
                    .Select(u => new RankingUsersModel
                    {
                        Id_Usuario = u.Id_Usuario,
                        Nombre = u.Nombre,
                        Gamer_Tag = u.Gamer_Tag,
                        Exp = u.Experiencia_Total,
                        Racha = u.Racha,
                        Rango_Actual = u.Rango_Actual
                    })
                    .ToList();

                var rachaTitulo = "🔥 Racha de Días";
                slides.Add(new RankingSlideModel
                {
                    Titulo = rachaTitulo,
                    Datos = rachaUsers,
                    Orden = ordenDeseado.GetValueOrDefault(rachaTitulo, ORDEN_POR_DEFECTO)
                });

                var actividades = _context.Pruebas
                    .Where(p => p.Activo)
                    .Where(p => p.Usuario != null && !rolesExcluidos.Contains(p.Usuario.Id_Rol))
                    .GroupBy(p => p.Tipo_Prueba)
                    .Select(grupo => new
                    {
                        NombreActividad = grupo.Key,
                        TopUsuariosIdsExp = grupo.GroupBy(p => p.Id_Usuario)
                            .Select(gUser => new
                            {
                                Id_Usuario = gUser.Key,
                                ExpTotal = gUser.Sum(x => x.ExperienciaAdquirida)
                            })
                            .OrderByDescending(u => u.ExpTotal)
                            .Take(cantidad > 0 ? cantidad : 100)
                            .ToList()
                    })
                    .ToList();

                foreach (var act in actividades)
                {
                    var userIds = act.TopUsuariosIdsExp.Select(u => u.Id_Usuario).ToList();

                    var userInfo = _context.Usuarios
                        .Where(u => userIds.Contains(u.Id_Usuario))
                        .Select(u => new { u.Id_Usuario, u.Nombre, u.Gamer_Tag, u.Rango_Actual })
                        .ToList();

                    var topUsuariosConRango = act.TopUsuariosIdsExp.Join(
                        userInfo,
                        top => top.Id_Usuario,
                        info => info.Id_Usuario,
                        (top, info) => new RankingUsersModel
                        {
                            Id_Usuario = top.Id_Usuario,
                            Nombre = info.Nombre,
                            Gamer_Tag = info.Gamer_Tag,
                            Exp = top.ExpTotal,
                            Racha = 0,
                            Rango_Actual = info.Rango_Actual
                        }
                    ).ToList();


                    int orden = ordenDeseado.GetValueOrDefault(act.NombreActividad, ORDEN_POR_DEFECTO);

                    slides.Add(new RankingSlideModel
                    {
                        Titulo = "📊 " + act.NombreActividad,
                        Datos = topUsuariosConRango,
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

            var rolesExcluidos = GetRolesExcluidosIds();

            var baseQuery = _context.Pruebas
                .Where(p => p.Activo && p.Fecha >= fechaInicio)
                .Where(p => p.Usuario != null && !rolesExcluidos.Contains(p.Usuario.Id_Rol));

            if (!actividad.Equals("Ranking Global", StringComparison.OrdinalIgnoreCase))
            {
                baseQuery = baseQuery.Where(p => p.Tipo_Prueba.Equals(actividad));
            }

            var topUsuariosIdsExp = baseQuery
                .GroupBy(p => p.Id_Usuario)
                .Select(gUser => new
                {
                    Id_Usuario = gUser.Key,
                    Exp = gUser.Sum(x => x.ExperienciaAdquirida)
                })
                .OrderByDescending(u => u.Exp)
                .Take(cantidad)
                .ToList();
            var userIds = topUsuariosIdsExp.Select(u => u.Id_Usuario).ToList();
            var userInfo = _context.Usuarios
                .Where(u => userIds.Contains(u.Id_Usuario))
                .Select(u => new { u.Id_Usuario, u.Nombre, u.Gamer_Tag, u.Rango_Actual })
                .ToList();

            var topUsuariosFiltrado = topUsuariosIdsExp.Join(
                userInfo,
                top => top.Id_Usuario,
                info => info.Id_Usuario,
                (top, info) => new RankingUsersModel
                {
                    Id_Usuario = top.Id_Usuario,
                    Nombre = info.Nombre,
                    Gamer_Tag = info.Gamer_Tag,
                    Exp = top.Exp,
                    Rango_Actual = info.Rango_Actual
                }
            ).ToList();

            string prefijo = actividad.Equals("Ranking Global", StringComparison.OrdinalIgnoreCase) ? "🌍" : "📊";
            string tituloFinal = actividad.Equals("Ranking Global", StringComparison.OrdinalIgnoreCase) ? "Ranking Global" : actividad;


            return new RankingSlideModel
            {
                Titulo = $"{prefijo} {tituloFinal} ({tituloPeriodo})",
                Datos = topUsuariosFiltrado
            };

        }


        //panel profes
        public List<UsuarioBDModel> ObtenerAlumnosPorClase(string nombreClase)
        {
            try
            {
                return _context.Usuarios
                    .Include(u => u.Rol)
                    .Include(u => u.Usuario_Clase)
                        .ThenInclude(uc => uc.Clase)
                    .Include(u => u.UsuarioLicencias)
                        .ThenInclude(ul => ul.Licencia)
                    .Where(u => u.Usuario_Clase.Any(uc => uc.Clase.Nombre == nombreClase)
                           && u.Rol.Rol == "Alumno")
                    .Select(u => new UsuarioBDModel
                    {
                        Id_Usuario = u.Id_Usuario,
                        Id_Rol = u.Rol.Rol,
                        Nombre = u.Nombre,
                        Correo = u.Correo,
                        Gamer_Tag = u.Gamer_Tag,
                        Pass = u.Pass,
                        Activo = u.Activo,
                        Racha = u.Racha,
                        Exp = u.Experiencia_Total,
                        Rango_Actual = u.Rango_Actual,
                        Ultima_Cnx = u.Ultima_Actividad,

                        Licencia = u.UsuarioLicencias
                            .Where(ul => ul.Licencia != null)
                            .Select(ul => ul.Licencia.Nombre)
                            .FirstOrDefault() ?? "Sin licencia",

                        Fecha_Asignacion_Licencia = u.UsuarioLicencias
                            .Select(ul => (DateTime?)ul.Fecha_Asignacion).FirstOrDefault(),

                        Fecha_Vencimiento_Licencia = u.UsuarioLicencias
                            .Select(ul => (DateTime?)ul.Fecha_Vencimiento).FirstOrDefault(),

                        Clases = u.Usuario_Clase
                            .Where(uc => uc.Clase != null)
                            .Select(uc => uc.Clase.Nombre)
                            .ToList()
                    })
                    .OrderByDescending(u => u.Ultima_Cnx)
                    .ToList();
            }
            catch (Exception ex)
            {
                return new List<UsuarioBDModel>();
            }
        }

        public UsuarioBDModel ObtenerAlumnoPorId(int idUsuario)
        {
            try
            {
                return _context.Usuarios
                    .Include(u => u.Rol)
                    .Include(u => u.Usuario_Clase)
                        .ThenInclude(uc => uc.Clase)
                    .Include(u => u.UsuarioLicencias)
                        .ThenInclude(ul => ul.Licencia)
                    .Where(u => u.Id_Usuario == idUsuario)
                    .Select(u => new UsuarioBDModel
                    {
                        Id_Usuario = u.Id_Usuario,
                        Id_Rol = u.Rol.Rol,
                        Nombre = u.Nombre,
                        Correo = u.Correo,
                        Gamer_Tag = u.Gamer_Tag,
                        Pass = u.Pass,
                        Activo = u.Activo,
                        Racha = u.Racha,
                        Exp = u.Experiencia_Total,
                        Rango_Actual = u.Rango_Actual,
                        Ultima_Cnx = u.Ultima_Actividad,

                        Licencia = u.UsuarioLicencias
                            .Where(ul => ul.Licencia != null)
                            .Select(ul => ul.Licencia.Nombre)
                            .FirstOrDefault() ?? "Sin licencia",

                        Fecha_Asignacion_Licencia = u.UsuarioLicencias
                            .FirstOrDefault() != null ? u.UsuarioLicencias.FirstOrDefault().Fecha_Asignacion : (DateTime?)null,

                        Fecha_Vencimiento_Licencia = u.UsuarioLicencias
                            .FirstOrDefault() != null ? u.UsuarioLicencias.FirstOrDefault().Fecha_Vencimiento : (DateTime?)null,

                        Clases = u.Usuario_Clase
                            .Where(uc => uc.Clase != null)
                            .Select(uc => uc.Clase.Nombre)
                            .ToList()
                    })
                    .FirstOrDefault();
            }
            catch (Exception)
            {
                return null;
            }
        }
        //panel profes

        //CRUD de usuarios para el panel de administración


        private static readonly List<(string Nombre, int MinExp)> Rangos = new List<(string, int)>
        {
                ("Materia Oscura III", 1000001),
                ("Materia Oscura II", 750000),
                ("Materia Oscura I", 500000),
                ("Heroico III", 300001),
                ("Heroico II", 280001),
                ("Heroico I", 260001),
                ("Diamante III", 220001),
                ("Diamante II", 200001),
                ("Diamante I", 180001),
                ("Oro III", 160001),
                ("Oro II", 140001),
                ("Oro I", 120001),
                ("Plata III", 100001),
                ("Plata II", 80001),
                ("Plata I", 60001),
                ("Bronce III", 40001),
                ("Bronce II", 20001),
                ("Bronce I", 0)
        };

        public string CalcularRango(int experienciaTotal)
        {
            var rangoEncontrado = Rangos.FirstOrDefault(r => experienciaTotal >= r.MinExp);
            return rangoEncontrado.Nombre ?? "Bronce I";
        }

        public string ActualizarDatosBasicosJugador(ActualizarUsuarioModel actualizar)
        {
            if (actualizar == null || actualizar.Id_Usuario <= 0)
            {
                return "Error: Datos de usuario inválidos";
            }

            actualizar.Nombre = actualizar.Nombre?.Trim();
            actualizar.Gamer_Tag = actualizar.Gamer_Tag?.Trim().Replace(" ", "");
            actualizar.Correo = actualizar.Correo?.Trim();
            actualizar.Pass = actualizar.Pass?.Trim();

            if (string.IsNullOrWhiteSpace(actualizar.Nombre) ||
                string.IsNullOrWhiteSpace(actualizar.Gamer_Tag) ||
                string.IsNullOrWhiteSpace(actualizar.Correo) ||
                string.IsNullOrWhiteSpace(actualizar.Pass))
            {
                return "Error: Ningún campo de texto puede estar vacío.";
            }

            if (actualizar.Exp < 0) actualizar.Exp = 0;
            if (actualizar.Exp > 2000000) actualizar.Exp = 2000000;

            if (actualizar.Racha < 0) actualizar.Racha = 0;
            if (actualizar.Racha > 4000) actualizar.Racha = 4000;

            try
            {

                bool colisionGamerTagOCorreo = _context.Usuarios.Any(u =>
                    u.Id_Usuario != actualizar.Id_Usuario &&
                    (u.Correo.ToLower() == actualizar.Correo || u.Gamer_Tag.ToLower() == actualizar.Gamer_Tag)
                );

                if (colisionGamerTagOCorreo)
                {
                    return "Error: El Correo o GamerTag ya está en uso por otro jugador.";
                }

                UsuariosDB usuarioDB = _context.Usuarios.FirstOrDefault(x => x.Id_Usuario == actualizar.Id_Usuario);

                if (usuarioDB == null)
                {
                    return "Error al actualizar: Jugador no encontrado.";
                }

                string nuevoRango = CalcularRango(actualizar.Exp);

                usuarioDB.Nombre = actualizar.Nombre;
                usuarioDB.Gamer_Tag = actualizar.Gamer_Tag;
                usuarioDB.Correo = actualizar.Correo;
                usuarioDB.Pass = actualizar.Pass;
                usuarioDB.Racha = actualizar.Racha;
                usuarioDB.Experiencia_Total = actualizar.Exp;
                usuarioDB.Rango_Actual = nuevoRango;


                _context.SaveChanges();
                return "Usuario actualizado correctamente";
            }
            catch (Exception ex)
            {
                return "Error al actualizar el usuario: " + ex.Message;
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
 
            var finalActivities = activities.Where(a => a.Activo == true).ToList();
            var objetivo = reto.ValorObjetivo.GetValueOrDefault();

            string tipoPruebaDB = GetDBStringForCountOrTime(reto.TipoActividad);

            switch (reto.TipoActividad)
            {
 
                case TipoActividadEnum.Efectividad100:
                case TipoActividadEnum.Efectividad90:
                case TipoActividadEnum.Efectividad80:
                    return finalActivities.Any(a =>
    
                        a.Tipo_Prueba.StartsWith(tipoPruebaDB) &&
                        a.Total_Preguntas > 0 &&
       
                        ((double)a.Respuestas_Correctas / a.Total_Preguntas * 100) >= objetivo);


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
   
                    return finalActivities.Count(a =>
                        a.Tipo_Prueba.StartsWith(tipoPruebaDB)) >= objetivo;


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
   
                    var totalTime = finalActivities
                        .Where(a => a.Tipo_Prueba.StartsWith(tipoPruebaDB))
                        .Sum(a => a.Tiempo.TotalMinutes);

                    return totalTime >= objetivo;


                case TipoActividadEnum.AcumularXP:

                    var totalXP = finalActivities.Sum(a => a.ExperienciaAdquirida);
                    return totalXP >= objetivo;

                case TipoActividadEnum.AcumularPreguntasCorrectas:

                    var totalRespuestas = finalActivities.Sum(a => a.Respuestas_Correctas);
                    return totalRespuestas >= objetivo;

                default:
                    return false;
            }
        }

        public HashSet<string> ObtenerModulosHabilitados(int idUsuario)
        {
            try
            {
                var usuario = _context.Usuarios.Find(idUsuario);
                if (usuario?.Id_Institucion == null)
                    return ObtenerTodosLosModulos();

                var modulos = _context.Instituciones_Modulos
                    .Where(m => m.Id_Institucion == usuario.Id_Institucion && m.Activo)
                    .Select(m => m.Clave_Modulo)
                    .ToHashSet();

                return modulos;
            }
            catch
            {
                return ObtenerTodosLosModulos();
            }
        }

        private HashSet<string> ObtenerTodosLosModulos()
        {
            return new HashSet<string>
            {
                "ranking", "mi_perfil", "conferencias", "hojas_ejercicios",
                "panel_profesor", "desafios", "memorizacion",
                "fingermath_lectura", "fingermath_escritura",
                "soroban_lectura", "soroban_escritura",
                "suma_resta", "flash_numeros", "flash_dictado",
                "multiplicacion_tablas", "multiplicacion_ejercicios",
                "multiplicacion_competencia", "division",
                "memoria_numero_figura", "memoria_rutas",
                "memoria_flash", "memoria_cartas",
                "desafio_calendario_competencia", "desafio_calendario_practica",
                "desafio_cuadros_competencia", "desafio_cuadros_practica",
                "desafio_potencias_competencia", "desafio_potencias_practica",
                "desafio_raices_competencia", "desafio_raices_practica",
                "desafio_dados"
            };
        }



    }
}
