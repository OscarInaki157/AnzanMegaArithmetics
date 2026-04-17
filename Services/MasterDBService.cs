using AnzanMegaArithmetics.Models;
using AnzanMegaArithmetics.Models.MasterModels;
using DataBase;
using Microsoft.EntityFrameworkCore;

namespace AnzanMegaArithmetics.Services
{
    public class MasterDBService : IMasterDBService
    {
        private readonly AnzanMegaContext _context;

        public MasterDBService(AnzanMegaContext context)
        {
            this._context = context;
        }

        public async Task<PanelMasterViewModel> ObtenerDatosDashboardAsync()
        {
            var modelo = new PanelMasterViewModel();


            modelo.DashboardIzquierdo.TotalEscuelas = await _context.Instituciones
                .CountAsync(i => i.Activo && i.Id_Institucion != 1);

            modelo.DashboardIzquierdo.TotalAlumnos = await _context.Usuarios
                .Include(u => u.Rol)
                .CountAsync(u => u.Activo && u.Rol.Rol == "Alumno");

            var inventarios = await _context.Instituciones_Inventario_Licencias.ToListAsync();

            int totalLicenciasCompradas = inventarios.Sum(i => i.Cantidad_Total);
            int totalLicenciasAsignadas = inventarios.Sum(i => i.Cantidad_Asignada);

            modelo.DashboardIzquierdo.TotalLicenciasGeneradas = totalLicenciasCompradas;
            modelo.DashboardIzquierdo.LicenciasAsignadas = totalLicenciasAsignadas;
            modelo.DashboardIzquierdo.LicenciasDisponibles = totalLicenciasCompradas - totalLicenciasAsignadas;

            var listaAlertas = new List<AlertaMasterModel>();

            foreach (var inv in inventarios)
            {
                if (inv.Id_Institucion == 1) continue;

                int licenciasLibres = inv.Cantidad_Total - inv.Cantidad_Asignada;

                if (licenciasLibres <= 10)
                {
                    var escuela = await _context.Instituciones.FindAsync(inv.Id_Institucion);

                    if (escuela != null && escuela.Activo)
                    {
                        listaAlertas.Add(new AlertaMasterModel
                        {
                            NombreInstitucion = escuela.Nombre,
                            MensajeAlerta = licenciasLibres == 0 ? "¡Sin licencias disponibles!" : $"Solo quedan {licenciasLibres} licencias",
                            TipoAlerta = licenciasLibres == 0 ? "Peligro" : "Advertencia"
                        });
                    }
                }
            }

            modelo.DashboardIzquierdo.Alertas = listaAlertas;

            // ==========================================
            // 4. DIRECTORIO DE INSTITUCIONES (Columna Derecha)
            // ==========================================
            var institucionesBD = await _context.Instituciones.ToListAsync();

            var listaInstituciones = new List<InstitucionDirectorioModel>();

            foreach (var inst in institucionesBD)
            {
                var inventario = inventarios.FirstOrDefault(i => i.Id_Institucion == inst.Id_Institucion);

                listaInstituciones.Add(new InstitucionDirectorioModel
                {
                    Id_Institucion = inst.Id_Institucion,
                    Nombre = inst.Nombre,
                    Activo = inst.Activo,
                    FechaRegistro = inst.Fecha_Registro,
                    LicenciasTotales = inventario?.Cantidad_Total ?? 0,
                    LicenciasUsadas = inventario?.Cantidad_Asignada ?? 0
                });
            }

            // Ordenamos: Mentes México (1) primero, las demás alfabéticamente
            modelo.ListaInstituciones = listaInstituciones
                .OrderBy(i => i.Id_Institucion != 1)
                .ThenBy(i => i.Nombre)
                .ToList();


            return modelo;
        }

        public async Task<(bool Exito, string Mensaje)> CrearInstitucionAsync(CrearInstitucionModel formModel)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                string nombreLimpio = formModel.Nombre.Trim();


                bool existe = await _context.Instituciones
                    .AnyAsync(i => i.Nombre.ToLower() == nombreLimpio.ToLower());

                if (existe)
                    return (false, $"La institución '{nombreLimpio}' ya existe.");

                var nuevaInstitucion = new InstitucionModel
                {
                    Nombre = nombreLimpio,
                    Activo = true,
                    Fecha_Registro = DateTime.Now
                };

                var dbInstitucion = new InstitucionesDB
                {
                    Nombre = nuevaInstitucion.Nombre,
                    Activo = nuevaInstitucion.Activo,
                    Fecha_Registro = nuevaInstitucion.Fecha_Registro
                };

                _context.Instituciones.Add(dbInstitucion);
                await _context.SaveChangesAsync();

                var nuevoInventario = new InventarioLicenciasModel
                {
                    Id_Institucion = dbInstitucion.Id_Institucion,
                    Id_Licencia = 1,
                    Cantidad_Total = formModel.LicenciasIniciales,
                    Cantidad_Asignada = 0
                };

                var dbInventario = new Instituciones_Inventario_LicenciasDB
                {
                    Id_Institucion = nuevoInventario.Id_Institucion,
                    Id_Licencia = nuevoInventario.Id_Licencia,
                    Cantidad_Total = nuevoInventario.Cantidad_Total,
                    Cantidad_Asignada = nuevoInventario.Cantidad_Asignada
                };

                _context.Instituciones_Inventario_Licencias.Add(dbInventario);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return (true, "Institución registrada correctamente.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, "Error interno: No se pudo completar el registro en la base de datos.");
            }
        }

        public async Task<GestionLicenciasModel> ObtenerDatosLicenciasAsync(int idInstitucion)
        {
            var inst = await _context.Instituciones.FindAsync(idInstitucion);
            var inv = await _context.Instituciones_Inventario_Licencias
                .FirstOrDefaultAsync(i => i.Id_Institucion == idInstitucion);

            return new GestionLicenciasModel
            {
                Id_Institucion = idInstitucion,
                NombreInstitucion = inst.Nombre,
                LicenciasTotales = inv?.Cantidad_Total ?? 0,
                LicenciasUsadas = inv?.Cantidad_Asignada ?? 0,
                NuevasLicenciasTotales = inv?.Cantidad_Total ?? 0
            };
        }

        public async Task<(bool Exito, string Mensaje)> ActualizarLicenciasAsync(GestionLicenciasModel model)
        {
            try
            {
                var inv = await _context.Instituciones_Inventario_Licencias
                    .FirstOrDefaultAsync(i => i.Id_Institucion == model.Id_Institucion);

                if (inv == null) return (false, "No se encontró el inventario de la institución.");

                // CANDADO DE SEGURIDAD: No se puede bajar de las licencias que ya tienen dueño
                if (model.NuevasLicenciasTotales < inv.Cantidad_Asignada)
                {
                    return (false, $"No puedes reducir a {model.NuevasLicenciasTotales} porque ya hay {inv.Cantidad_Asignada} licencias asignadas a usuarios.");
                }

                inv.Cantidad_Total = model.NuevasLicenciasTotales;
                await _context.SaveChangesAsync();

                return (true, "Inventario actualizado correctamente.");
            }
            catch (Exception)
            {
                return (false, "Error al actualizar la base de datos.");
            }
        }

        public async Task<DetalleInstitucionViewModel> ObtenerDetalleInstitucionAsync(int id)
        {
            var inst = await _context.Instituciones.FindAsync(id);
            if (inst == null) return null;

            var inv = await _context.Instituciones_Inventario_Licencias
                .FirstOrDefaultAsync(i => i.Id_Institucion == id);

            var totalAlumnos = await _context.Usuarios
                .Include(u => u.Rol)
                .CountAsync(u => u.Id_Institucion == id && u.Rol.Rol == "Alumno");

            var totalProfes = await _context.Usuarios
                .Include(u => u.Rol)
                .CountAsync(u => u.Id_Institucion == id && u.Rol.Rol == "Profesor");

            var totalClases = await _context.Clases
                .CountAsync(c => c.Id_Institucion == id);

            DateTime fechaVencimiento = inst.Fecha_Registro.AddYears(1);
            DateTime hoy = DateTime.Now;

            int diasTotalesContrato = (fechaVencimiento - inst.Fecha_Registro).Days;
            int diasRestantes = (fechaVencimiento - hoy).Days;
            diasRestantes = diasRestantes < 0 ? 0 : diasRestantes;

            int porcentajeTranscurrido = 100 - (int)((double)diasRestantes / diasTotalesContrato * 100);
            porcentajeTranscurrido = Math.Clamp(porcentajeTranscurrido, 0, 100);

            DateTime fechaCorteActividad = hoy.AddDays(-28);

            var datosPruebas = await _context.Pruebas
                .Where(p => p.Usuario.Id_Institucion == id && p.Activo)
                .Select(p => new { p.Fecha, p.Respuestas_Correctas, p.Total_Preguntas })
                .ToListAsync();


            var fechasRecientes = datosPruebas
                .Where(p => p.Fecha >= fechaCorteActividad)
                .Select(p => p.Fecha)
                .ToList();

            var labelsActividad = new List<string>();
            var valoresActividad = new List<int>();

            for (int i = 3; i >= 0; i--)
            {
                DateTime inicioSemana = hoy.AddDays(-(i + 1) * 7);
                DateTime finSemana = hoy.AddDays(-i * 7);

                int conteo = fechasRecientes.Count(f => f > inicioSemana && f <= finSemana);

                if (i == 0) labelsActividad.Add("Esta semana");
                else if (i == 1) labelsActividad.Add("Semana pasada");
                else labelsActividad.Add($"Hace {i} semanas");

                valoresActividad.Add(conteo);
            }

            // CÁLCULO DEL PROMEDIO GENERAL DE LA ESCUELA
            double promedioReal = 0;
            long totalPreguntasEscuela = datosPruebas.Sum(p => (long)p.Total_Preguntas);
            long totalAciertosEscuela = datosPruebas.Sum(p => (long)p.Respuestas_Correctas);

            if (totalPreguntasEscuela > 0)
            {
                promedioReal = Math.Round((double)totalAciertosEscuela / totalPreguntasEscuela * 100, 1);
            }

            var clasesDB = await _context.Clases
            .Where(c => c.Id_Institucion == id)
            .ToListAsync();

            var listaClasesSede = new List<ClaseSedeModel>();

            foreach (var c in clasesDB)
            {
                int alumnosEnClase = await _context.Usuarios
                    .Where(u => u.Id_Institucion == id && u.Usuario_Clase.Any(uc => uc.Id_Clase == c.Id_Clase))
                    .CountAsync();

                listaClasesSede.Add(new ClaseSedeModel
                {
                    Id_Clase = c.Id_Clase,
                    Nombre = c.Nombre,
                    Activa = c.Activo,
                    CantidadAlumnos = alumnosEnClase
                });
            }

            return new DetalleInstitucionViewModel
            {
                Id_Institucion = inst.Id_Institucion,
                Nombre = inst.Nombre,
                FechaRegistro = inst.Fecha_Registro,
                FechaVencimiento = fechaVencimiento,
                Activa = inst.Activo,

                TotalLicencias = inv?.Cantidad_Total ?? 0,
                LicenciasEnUso = inv?.Cantidad_Asignada ?? 0,
                DiasRestantes = diasRestantes,
                PorcentajeTiempoTranscurrido = porcentajeTranscurrido,

                TotalAlumnos = totalAlumnos,
                TotalProfesores = totalProfes,
                TotalClases = totalClases,

                PromedioGeneral = promedioReal,
                LabelsActividad = labelsActividad,
                ValoresActividad = valoresActividad,

                ListaClases = listaClasesSede
            };
        }

        public async Task<EditarInstitucionModel> ObtenerInstitucionParaEdicionAsync(int id)
        {
            var inst = await _context.Instituciones.FindAsync(id);
            if (inst == null) return null;

            return new EditarInstitucionModel
            {
                Id_Institucion = inst.Id_Institucion,
                NombreActual = inst.Nombre,
                NuevoNombre = inst.Nombre
            };
        }

        public async Task<(bool Exito, string Mensaje)> EditarNombreInstitucionAsync(int id, string nuevoNombre)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(nuevoNombre))
                    return (false, "El nombre de la institución no puede estar vacío.");

                string nombreLimpio = nuevoNombre.Trim();

                if (nombreLimpio.Length < 3)
                    return (false, "El nombre debe tener al menos 3 caracteres.");

                bool existeOtra = await _context.Instituciones
                    .AnyAsync(i => i.Nombre.ToLower() == nombreLimpio.ToLower() && i.Id_Institucion != id);

                if (existeOtra)
                    return (false, $"Ya existe OTRA institución registrada como '{nombreLimpio}'.");

                var inst = await _context.Instituciones.FindAsync(id);
                if (inst == null)
                    return (false, "La institución no existe o fue eliminada.");

                // Solo cambiamos el valor. ¡EF Core hace el resto mágicamente!
                inst.Nombre = nombreLimpio;

                await _context.SaveChangesAsync();

                return (true, "Institución actualizada correctamente.");
            }
            catch (Exception ex)
            {
                // Así sabremos la verdad si hay un error de base de datos
                return (false, $"Error interno: {ex.Message}");
            }
        }

        public async Task<(bool Exito, string Mensaje)> CrearClaseAsync(CrearClaseModel model)
        {
            try
            {
                string nombreLimpio = model.Nombre.Trim();

                bool existe = await _context.Clases
                    .AnyAsync(c => c.Id_Institucion == model.Id_Institucion
                                && c.Nombre.ToLower() == nombreLimpio.ToLower());

                if (existe)
                    return (false, $"Ya existe un salón llamado '{nombreLimpio}' en esta institución.");

                var nuevaClase = new ClaseDB
                {
                    Nombre = nombreLimpio,
                    Id_Institucion = model.Id_Institucion,
                    Activo = true
                };

                _context.Clases.Add(nuevaClase);
                await _context.SaveChangesAsync();

                return (true, "Salón creado correctamente.");
            }
            catch (Exception ex)
            {
                return (false, $"Error interno: {ex.Message}");
            }
        }

        public async Task<EditarClaseModel?> ObtenerClaseParaEdicionAsync(int idClase)
        {
            var clase = await _context.Clases.FindAsync(idClase);
            if (clase == null) return null;

            return new EditarClaseModel
            {
                Id_Clase = clase.Id_Clase,
                NombreActual = clase.Nombre,
                NuevoNombre = clase.Nombre
            };
        }

        public async Task<(bool Exito, string Mensaje)> EditarNombreClaseAsync(EditarClaseModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.NuevoNombre))
                    return (false, "El nombre no puede estar vacío.");

                string nombreLimpio = model.NuevoNombre.Trim();

                var clase = await _context.Clases.FindAsync(model.Id_Clase);
                if (clase == null)
                    return (false, "El salón no existe o fue eliminado.");

                // Verificar duplicado dentro de la misma institución (excluyendo este mismo)
                bool existeOtra = await _context.Clases
                    .AnyAsync(c => c.Id_Institucion == clase.Id_Institucion
                                && c.Nombre.ToLower() == nombreLimpio.ToLower()
                                && c.Id_Clase != model.Id_Clase);

                if (existeOtra)
                    return (false, $"Ya existe otro salón llamado '{nombreLimpio}' en esta institución.");

                clase.Nombre = nombreLimpio;
                await _context.SaveChangesAsync();

                return (true, "Salón actualizado correctamente.");
            }
            catch (Exception ex)
            {
                return (false, $"Error interno: {ex.Message}");
            }
        }

        public async Task<ListadoAlumnosInstitucionViewModel> ObtenerAlumnosInstitucionAsync(int idInstitucion, string clase = "Todas")
        {
            var alumnosQuery = _context.Usuarios
                .Include(u => u.Rol)
                .Include(u => u.Usuario_Clase).ThenInclude(uc => uc.Clase)
                .Include(u => u.UsuarioLicencias).ThenInclude(ul => ul.Licencia)
                .Where(u => u.Id_Institucion == idInstitucion && u.Rol.Rol == "Alumno");

            if (clase != "Todas")
                alumnosQuery = alumnosQuery.Where(u =>
                    u.Usuario_Clase.Any(uc => uc.Clase.Nombre == clase && uc.Activo));

            var alumnosBD = await alumnosQuery.ToListAsync();

            var clasesDeSede = await _context.Clases
                .Where(c => c.Id_Institucion == idInstitucion && c.Activo)
                .Select(c => c.Nombre)
                .ToListAsync();

            var clases = new List<string> { "Todas" };
            clases.AddRange(clasesDeSede);

            var lista = alumnosBD.Select(u =>
            {
                var licencia = u.UsuarioLicencias
                    .OrderByDescending(ul => ul.Fecha_Vencimiento)
                    .FirstOrDefault();

                return new UsuarioBDModel
                {
                    Id_Usuario = u.Id_Usuario,
                    Nombre = u.Nombre,
                    Gamer_Tag = u.Gamer_Tag,
                    Correo = u.Correo,
                    Pass = u.Pass,
                    Activo = u.Activo,
                    Racha = u.Racha,
                    Exp = u.Experiencia_Total,
                    Rango_Actual = u.Rango_Actual,
                    Ultima_Cnx = u.Ultima_Actividad,
                    Clases = u.Usuario_Clase
                                    .Where(uc => uc.Activo)
                                    .Select(uc => uc.Clase.Nombre)
                                    .ToList(),
                    Licencia = licencia?.Licencia.Nombre ?? "Sin licencia",
                    Fecha_Asignacion_Licencia = licencia?.Fecha_Asignacion,
                    Fecha_Vencimiento_Licencia = licencia?.Fecha_Vencimiento
                };
            }).ToList();

            return new ListadoAlumnosInstitucionViewModel
            {
                Id_Institucion = idInstitucion,
                ListaAlumnos = lista,
                ClasesDisponibles = clases,
                ClaseActual = clase
            };
        }

        public async Task<UsuarioBDModel> ObtenerFichaAlumnoAsync(int idUsuario)
        {
            var u = await _context.Usuarios
                .Include(u => u.Rol)
                .Include(u => u.Usuario_Clase).ThenInclude(uc => uc.Clase)
                .Include(u => u.UsuarioLicencias).ThenInclude(ul => ul.Licencia)
                .FirstOrDefaultAsync(u => u.Id_Usuario == idUsuario);

            if (u == null) return null;

            var licencia = u.UsuarioLicencias
                .OrderByDescending(ul => ul.Fecha_Vencimiento)
                .FirstOrDefault();

            return new UsuarioBDModel
            {
                Id_Usuario = u.Id_Usuario,
                Nombre = u.Nombre,
                Gamer_Tag = u.Gamer_Tag,
                Correo = u.Correo,
                Pass = u.Pass,
                Activo = u.Activo,
                Racha = u.Racha,
                Exp = u.Experiencia_Total,
                Rango_Actual = u.Rango_Actual,
                Ultima_Cnx = u.Ultima_Actividad,
                Clases = u.Usuario_Clase
                                .Where(uc => uc.Activo)
                                .Select(uc => uc.Clase.Nombre)
                                .ToList(),
                Licencia = licencia?.Licencia.Nombre ?? "Sin licencia",
                Fecha_Asignacion_Licencia = licencia?.Fecha_Asignacion,
                Fecha_Vencimiento_Licencia = licencia?.Fecha_Vencimiento
            };
        }

        public async Task<CrearAlumnoMasterModel> ObtenerFormularioCrearAlumnoAsync(int idInstitucion)
        {
            var clases = await _context.Clases
                .Where(c => c.Id_Institucion == idInstitucion && c.Activo)
                .Select(c => new ClaseSedeModel { Id_Clase = c.Id_Clase, Nombre = c.Nombre })
                .ToListAsync();

            var inventario = await _context.Instituciones_Inventario_Licencias
                .FirstOrDefaultAsync(i => i.Id_Institucion == idInstitucion);

            int disponibles = inventario != null
                ? inventario.Cantidad_Total - inventario.Cantidad_Asignada
                : 0;

            return new CrearAlumnoMasterModel
            {
                Id_Institucion = idInstitucion,
                ClasesDisponibles = clases,
                LicenciasDisponibles = disponibles
            };
        }

        public async Task<(bool Exito, string Mensaje)> CrearAlumnoMasterAsync(CrearAlumnoMasterModel model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Correo único global
                bool correoExiste = await _context.Usuarios
                    .AnyAsync(u => u.Correo.ToLower() == model.Correo.Trim().ToLower());
                if (correoExiste)
                    return (false, "Ese correo ya está registrado en la plataforma.");

                // 2. GamerTag único global
                bool gtExiste = await _context.Usuarios
                    .AnyAsync(u => u.Gamer_Tag.ToLower() == model.Gamer_Tag.Trim().ToLower());
                if (gtExiste)
                    return (false, "Ese GamerTag ya está en uso.");

                // 3. Verificar que la clase pertenece a esta institución (evita manipulación)
                bool claseValida = await _context.Clases
                    .AnyAsync(c => c.Id_Clase == model.Id_Clase
                                && c.Id_Institucion == model.Id_Institucion
                                && c.Activo);
                if (!claseValida)
                    return (false, "La clase seleccionada no pertenece a esta institución.");

                // 4. Verificar licencias disponibles (doble candado, por si acaso)
                var inventario = await _context.Instituciones_Inventario_Licencias
                    .FirstOrDefaultAsync(i => i.Id_Institucion == model.Id_Institucion);

                if (inventario == null || inventario.Cantidad_Asignada >= inventario.Cantidad_Total)
                    return (false, "No hay licencias disponibles. Aumenta el inventario primero.");

                // 5. Crear usuario
                var nuevoUsuario = new UsuariosDB
                {
                    Id_Rol = 1, // Alumno
                    Id_Institucion = model.Id_Institucion,
                    Nombre = model.Nombre.Trim(),
                    Correo = model.Correo.Trim().ToLower(),
                    Gamer_Tag = model.Gamer_Tag.Trim(),
                    Pass = model.Pass,
                    Activo = true,
                    Racha = 0,
                    Experiencia_Total = 0,
                    Rango_Actual = "Bronce I",
                    Ultima_Actividad = DateTime.Now,
                    Fecha_Ultimo_Reclamo = DateTime.Now.AddDays(-1)
                };

                _context.Usuarios.Add(nuevoUsuario);
                await _context.SaveChangesAsync();

                // 6. Asignar clase
                _context.Usuarios_Clases.Add(new Usuario_ClaseDB
                {
                    Id_Usuario = nuevoUsuario.Id_Usuario,
                    Id_Clase = model.Id_Clase,
                    Activo = true
                });

                // 7. Asignar licencia y descontar del inventario
                _context.Usuarios_Licencias.Add(new Usuarios_LicenciasDB
                {
                    Id_Usuario = nuevoUsuario.Id_Usuario,
                    Id_Licencia = inventario.Id_Licencia,
                    Fecha_Asignacion = DateTime.Now,
                    Fecha_Vencimiento = DateTime.Now.AddMonths(12),
                    Vigencia = 12
                });

                inventario.Cantidad_Asignada++;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, $"Alumno '{nuevoUsuario.Nombre}' creado correctamente.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Error interno: {ex.Message}");
            }
        }

        public async Task<EditarAlumnoMasterModel> ObtenerDatosEditarAlumnoMasterAsync(int idUsuario)
        {
            var u = await _context.Usuarios
                .Include(u => u.Usuario_Clase).ThenInclude(uc => uc.Clase)
                .Include(u => u.UsuarioLicencias).ThenInclude(ul => ul.Licencia)
                .FirstOrDefaultAsync(u => u.Id_Usuario == idUsuario);

            if (u == null) return null;

            var clases = await _context.Clases
                .Where(c => c.Id_Institucion == u.Id_Institucion && c.Activo)
                .Select(c => new ClaseSedeModel { Id_Clase = c.Id_Clase, Nombre = c.Nombre })
                .ToListAsync();

            var claseActual = u.Usuario_Clase.FirstOrDefault(uc => uc.Activo);

            var licencia = u.UsuarioLicencias
                .OrderByDescending(ul => ul.Fecha_Vencimiento)
                .FirstOrDefault();

            return new EditarAlumnoMasterModel
            {
                Id_Usuario = u.Id_Usuario,
                Nombre = u.Nombre,
                Gamer_Tag = u.Gamer_Tag,
                Correo = u.Correo,
                Pass = u.Pass,
                Racha = u.Racha,
                Exp = u.Experiencia_Total,
                Activo = u.Activo,
                Id_Clase_Actual = claseActual?.Id_Clase,
                Id_Clase_Nueva = claseActual?.Id_Clase,
                ClasesDisponibles = clases,
                LicenciaActual = licencia?.Licencia.Nombre ?? "Sin licencia",
                Id_UsuarioLicencia = licencia?.Id,
                Fecha_Inicio_Licencia = licencia?.Fecha_Asignacion,
                Fecha_Fin_Licencia = licencia?.Fecha_Vencimiento
            };
        }

        public async Task<(bool Exito, string Mensaje)> GuardarEdicionAlumnoMasterAsync(EditarAlumnoMasterModel model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var u = await _context.Usuarios
                    .Include(u => u.Usuario_Clase)
                    .FirstOrDefaultAsync(u => u.Id_Usuario == model.Id_Usuario);

                if (u == null) return (false, "Usuario no encontrado.");

                bool gtDuplicado = await _context.Usuarios
                    .AnyAsync(x => x.Gamer_Tag == model.Gamer_Tag && x.Id_Usuario != model.Id_Usuario);
                if (gtDuplicado) return (false, "Ese GamerTag ya está en uso.");

                bool correoDuplicado = await _context.Usuarios
                    .AnyAsync(x => x.Correo == model.Correo && x.Id_Usuario != model.Id_Usuario);
                if (correoDuplicado) return (false, "Ese correo ya está registrado.");

                u.Nombre = model.Nombre.Trim();
                u.Gamer_Tag = model.Gamer_Tag.Trim();
                u.Correo = model.Correo.Trim();
                u.Pass = model.Pass;
                u.Racha = model.Racha;
                u.Experiencia_Total = model.Exp;
                u.Activo = model.Activo;

                // Cambio de clase: ELIMINAR la anterior y CREAR la nueva
                if (model.Id_Clase_Nueva.HasValue)
                {
                    var registrosActuales = await _context.Usuarios_Clases
                        .Where(uc => uc.Id_Usuario == model.Id_Usuario)
                        .ToListAsync();

                    // Eliminar todos los que no sean la clase nueva
                    var aEliminar = registrosActuales
                        .Where(uc => uc.Id_Clase != model.Id_Clase_Nueva.Value)
                        .ToList();
                    _context.Usuarios_Clases.RemoveRange(aEliminar);

                    // Crear o reactivar la clase nueva
                    var ucNueva = registrosActuales
                        .FirstOrDefault(uc => uc.Id_Clase == model.Id_Clase_Nueva.Value);

                    if (ucNueva == null)
                        _context.Usuarios_Clases.Add(new Usuario_ClaseDB
                        {
                            Id_Usuario = model.Id_Usuario,
                            Id_Clase = model.Id_Clase_Nueva.Value,
                            Activo = true
                        });
                    else
                        ucNueva.Activo = true;
                }

                if (model.Id_UsuarioLicencia.HasValue &&
                    model.Fecha_Inicio_Licencia.HasValue &&
                    model.Fecha_Fin_Licencia.HasValue)
                {
                    var lic = await _context.Usuarios_Licencias
                        .FindAsync(model.Id_UsuarioLicencia.Value);

                    if (lic != null)
                    {
                        if (model.Fecha_Fin_Licencia <= model.Fecha_Inicio_Licencia)
                            return (false, "La fecha de vencimiento debe ser posterior a la de inicio.");

                        lic.Fecha_Asignacion = model.Fecha_Inicio_Licencia.Value;
                        lic.Fecha_Vencimiento = model.Fecha_Fin_Licencia.Value;
                        lic.Vigencia = (int)Math.Ceiling(
                            (model.Fecha_Fin_Licencia.Value - model.Fecha_Inicio_Licencia.Value).TotalDays / 30
                        );
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return (true, "Alumno actualizado correctamente.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Error interno: {ex.Message}");
            }
        }

        public async Task<(bool Exito, string Mensaje)> ToggleActivoAlumnoAsync(int idUsuario)
        {
            try
            {
                var u = await _context.Usuarios.FindAsync(idUsuario);
                if (u == null) return (false, "Usuario no encontrado.");

                u.Activo = !u.Activo;
                await _context.SaveChangesAsync();

                return (true, u.Activo ? "Alumno activado." : "Alumno desactivado.");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        public async Task<(bool Exito, string Mensaje)> ToggleActivoClaseAsync(int idClase)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var clase = await _context.Clases.FindAsync(idClase);
                if (clase == null) return (false, "Clase no encontrada.");

                // Obtener todos los usuarios activos en esta clase
                var usuariosEnClase = await _context.Usuarios_Clases
                    .Include(uc => uc.Usuario)
                    .Where(uc => uc.Id_Clase == idClase && uc.Activo)
                    .Select(uc => uc.Usuario)
                    .ToListAsync();

                // Determinar acción: si todos están activos, desactivar; si alguno inactivo, activar todos
                bool todosActivos = usuariosEnClase.All(u => u.Activo);
                bool nuevoEstado = !todosActivos;

                foreach (var u in usuariosEnClase)
                    u.Activo = nuevoEstado;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                string accion = nuevoEstado ? "activados" : "desactivados";
                return (true, $"{usuariosEnClase.Count} usuarios {accion} correctamente.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Error interno: {ex.Message}");
            }
        }

        public async Task<ListadoProfesoresInstitucionViewModel> ObtenerProfesoresInstitucionAsync(int idInstitucion)
        {
            var profesoresBD = await _context.Usuarios
                .Include(u => u.Rol)
                .Include(u => u.Usuario_Clase).ThenInclude(uc => uc.Clase)
                .Include(u => u.UsuarioLicencias).ThenInclude(ul => ul.Licencia)
                .Where(u => u.Id_Institucion == idInstitucion && u.Rol.Rol == "Profesor")
                .ToListAsync();

            var lista = profesoresBD.Select(u =>
            {
                var licencia = u.UsuarioLicencias
                    .OrderByDescending(ul => ul.Fecha_Vencimiento)
                    .FirstOrDefault();

                return new UsuarioBDModel
                {
                    Id_Usuario = u.Id_Usuario,
                    Nombre = u.Nombre,
                    Gamer_Tag = u.Gamer_Tag,
                    Correo = u.Correo,
                    Pass = u.Pass,
                    Activo = u.Activo,
                    Racha = u.Racha,
                    Exp = u.Experiencia_Total,
                    Rango_Actual = u.Rango_Actual,
                    Ultima_Cnx = u.Ultima_Actividad,
                    Clases = u.Usuario_Clase
                                    .Where(uc => uc.Activo)
                                    .Select(uc => uc.Clase.Nombre)
                                    .ToList(),
                    Licencia = licencia?.Licencia.Nombre ?? "Sin licencia",
                    Fecha_Asignacion_Licencia = licencia?.Fecha_Asignacion,
                    Fecha_Vencimiento_Licencia = licencia?.Fecha_Vencimiento
                };
            }).ToList();

            return new ListadoProfesoresInstitucionViewModel
            {
                Id_Institucion = idInstitucion,
                ListaProfesores = lista
            };
        }

        public async Task<CrearProfesorMasterModel> ObtenerFormularioCrearProfesorAsync(int idInstitucion)
        {
            var clases = await _context.Clases
                .Where(c => c.Id_Institucion == idInstitucion && c.Activo)
                .Select(c => new ClaseSedeModel { Id_Clase = c.Id_Clase, Nombre = c.Nombre })
                .ToListAsync();

            var inventario = await _context.Instituciones_Inventario_Licencias
                .FirstOrDefaultAsync(i => i.Id_Institucion == idInstitucion);

            int disponibles = inventario != null
                ? inventario.Cantidad_Total - inventario.Cantidad_Asignada
                : 0;

            return new CrearProfesorMasterModel
            {
                Id_Institucion = idInstitucion,
                ClasesDisponibles = clases,
                LicenciasDisponibles = disponibles
            };
        }

        public async Task<(bool Exito, string Mensaje)> CrearProfesorMasterAsync(CrearProfesorMasterModel model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Validaciones
                bool correoExiste = await _context.Usuarios
                    .AnyAsync(u => u.Correo.ToLower() == model.Correo.Trim().ToLower());
                if (correoExiste)
                    return (false, "Ese correo ya está registrado en la plataforma.");

                bool gtExiste = await _context.Usuarios
                    .AnyAsync(u => u.Gamer_Tag.ToLower() == model.Gamer_Tag.Trim().ToLower());
                if (gtExiste)
                    return (false, "Ese GamerTag ya está en uso.");

                if (model.Ids_Clases == null || !model.Ids_Clases.Any())
                    return (false, "Debes asignar al menos una clase al profesor.");

                // Validar que todas las clases pertenecen a la institución
                foreach (var idClase in model.Ids_Clases)
                {
                    bool claseValida = await _context.Clases
                        .AnyAsync(c => c.Id_Clase == idClase
                                    && c.Id_Institucion == model.Id_Institucion
                                    && c.Activo);
                    if (!claseValida)
                        return (false, "Una o más clases seleccionadas no pertenecen a esta institución.");
                }

                // Verificar licencias
                var inventario = await _context.Instituciones_Inventario_Licencias
                    .FirstOrDefaultAsync(i => i.Id_Institucion == model.Id_Institucion);

                if (inventario == null || inventario.Cantidad_Asignada >= inventario.Cantidad_Total)
                    return (false, "No hay licencias disponibles. Aumenta el inventario primero.");

                // Crear profesor — Id_Rol = 2
                var nuevoProfesor = new UsuariosDB
                {
                    Id_Rol = 2,
                    Id_Institucion = model.Id_Institucion,
                    Nombre = model.Nombre.Trim(),
                    Correo = model.Correo.Trim().ToLower(),
                    Gamer_Tag = model.Gamer_Tag.Trim(),
                    Pass = model.Pass,
                    Activo = true,
                    Racha = 0,
                    Experiencia_Total = 0,
                    Rango_Actual = "Bronce I",
                    Ultima_Actividad = DateTime.Now,
                    Fecha_Ultimo_Reclamo = DateTime.Now.AddDays(-1)
                };

                _context.Usuarios.Add(nuevoProfesor);
                await _context.SaveChangesAsync();

                // Asignar múltiples clases
                foreach (var idClase in model.Ids_Clases)
                {
                    _context.Usuarios_Clases.Add(new Usuario_ClaseDB
                    {
                        Id_Usuario = nuevoProfesor.Id_Usuario,
                        Id_Clase = idClase,
                        Activo = true
                    });
                }

                // Asignar licencia
                var licenciaDB = await _context.Licencias.FindAsync(inventario.Id_Licencia);
                int vigencia = licenciaDB?.Vigencia ?? 12;

                _context.Usuarios_Licencias.Add(new Usuarios_LicenciasDB
                {
                    Id_Usuario = nuevoProfesor.Id_Usuario,
                    Id_Licencia = inventario.Id_Licencia,
                    Fecha_Asignacion = DateTime.Now,
                    Fecha_Vencimiento = DateTime.Now.AddMonths(vigencia),
                    Vigencia = vigencia
                });

                inventario.Cantidad_Asignada++;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, $"Profesor '{nuevoProfesor.Nombre}' creado correctamente.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Error interno: {ex.Message}");
            }
        }

        public async Task<EditarProfesorMasterModel> ObtenerDatosEditarProfesorMasterAsync(int idUsuario)
        {
            var u = await _context.Usuarios
                .Include(u => u.Usuario_Clase).ThenInclude(uc => uc.Clase)
                .Include(u => u.UsuarioLicencias).ThenInclude(ul => ul.Licencia)
                .FirstOrDefaultAsync(u => u.Id_Usuario == idUsuario);

            if (u == null) return null;

            var clases = await _context.Clases
                .Where(c => c.Id_Institucion == u.Id_Institucion && c.Activo)
                .Select(c => new ClaseSedeModel { Id_Clase = c.Id_Clase, Nombre = c.Nombre })
                .ToListAsync();

            var licencia = u.UsuarioLicencias
                .OrderByDescending(ul => ul.Fecha_Vencimiento)
                .FirstOrDefault();

            return new EditarProfesorMasterModel
            {
                Id_Usuario = u.Id_Usuario,
                Nombre = u.Nombre,
                Gamer_Tag = u.Gamer_Tag,
                Correo = u.Correo,
                Pass = u.Pass,
                Racha = u.Racha,
                Exp = u.Experiencia_Total,
                Activo = u.Activo,
                Ids_Clases_Actuales = u.Usuario_Clase.Where(uc => uc.Activo).Select(uc => uc.Id_Clase).ToList(),
                Ids_Clases_Nuevas = u.Usuario_Clase.Where(uc => uc.Activo).Select(uc => uc.Id_Clase).ToList(),
                ClasesDisponibles = clases,
                LicenciaActual = licencia?.Licencia.Nombre ?? "Sin licencia",
                Id_UsuarioLicencia = licencia?.Id,
                Fecha_Inicio_Licencia = licencia?.Fecha_Asignacion,
                Fecha_Fin_Licencia = licencia?.Fecha_Vencimiento
            };
        }

        public async Task<(bool Exito, string Mensaje)> GuardarEdicionProfesorMasterAsync(EditarProfesorMasterModel model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var u = await _context.Usuarios
                    .Include(u => u.Usuario_Clase)
                    .FirstOrDefaultAsync(u => u.Id_Usuario == model.Id_Usuario);

                if (u == null) return (false, "Usuario no encontrado.");

                bool gtDuplicado = await _context.Usuarios
                    .AnyAsync(x => x.Gamer_Tag == model.Gamer_Tag && x.Id_Usuario != model.Id_Usuario);
                if (gtDuplicado) return (false, "Ese GamerTag ya está en uso.");

                bool correoDuplicado = await _context.Usuarios
                    .AnyAsync(x => x.Correo == model.Correo && x.Id_Usuario != model.Id_Usuario);
                if (correoDuplicado) return (false, "Ese correo ya está registrado.");

                if (model.Ids_Clases_Nuevas == null || !model.Ids_Clases_Nuevas.Any())
                    return (false, "El profesor debe tener al menos una clase asignada.");

                // Validar que las clases pertenecen a la institución
                foreach (var idClase in model.Ids_Clases_Nuevas)
                {
                    bool valida = await _context.Clases
                        .AnyAsync(c => c.Id_Clase == idClase
                                    && c.Id_Institucion == u.Id_Institucion
                                    && c.Activo);
                    if (!valida)
                        return (false, "Una o más clases seleccionadas no son válidas.");
                }

                // Actualizar datos básicos
                u.Nombre = model.Nombre.Trim();
                u.Gamer_Tag = model.Gamer_Tag.Trim();
                u.Correo = model.Correo.Trim();
                u.Pass = model.Pass;
                u.Racha = model.Racha;
                u.Experiencia_Total = model.Exp;
                u.Activo = model.Activo;

                // Reasignar clases: desactivar todas y reactivar las seleccionadas
                var registrosActuales = await _context.Usuarios_Clases
                    .Where(uc => uc.Id_Usuario == model.Id_Usuario)
                    .ToListAsync();

                // Eliminar los que ya no están seleccionados
                var aEliminar = registrosActuales
                    .Where(uc => !model.Ids_Clases_Nuevas.Contains(uc.Id_Clase))
                    .ToList();
                _context.Usuarios_Clases.RemoveRange(aEliminar);

                // Insertar los que no existen aún
                var idsExistentes = registrosActuales.Select(uc => uc.Id_Clase).ToList();
                foreach (var idClase in model.Ids_Clases_Nuevas)
                {
                    if (!idsExistentes.Contains(idClase))
                    {
                        _context.Usuarios_Clases.Add(new Usuario_ClaseDB
                        {
                            Id_Usuario = model.Id_Usuario,
                            Id_Clase = idClase,
                            Activo = true
                        });
                    }
                    else
                    {
                        // Si existe pero estaba inactivo, reactivarlo
                        var ucExistente = registrosActuales.FirstOrDefault(uc => uc.Id_Clase == idClase);
                        if (ucExistente != null) ucExistente.Activo = true;
                    }
                }

                // Actualizar licencia
                if (model.Id_UsuarioLicencia.HasValue &&
                    model.Fecha_Inicio_Licencia.HasValue &&
                    model.Fecha_Fin_Licencia.HasValue)
                {
                    if (model.Fecha_Fin_Licencia <= model.Fecha_Inicio_Licencia)
                        return (false, "La fecha de vencimiento debe ser posterior a la de inicio.");

                    var lic = await _context.Usuarios_Licencias.FindAsync(model.Id_UsuarioLicencia.Value);
                    if (lic != null)
                    {
                        lic.Fecha_Asignacion = model.Fecha_Inicio_Licencia.Value;
                        lic.Fecha_Vencimiento = model.Fecha_Fin_Licencia.Value;
                        lic.Vigencia = (int)Math.Ceiling(
                            (model.Fecha_Fin_Licencia.Value - model.Fecha_Inicio_Licencia.Value).TotalDays / 30
                        );
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return (true, "Profesor actualizado correctamente.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Error interno: {ex.Message}");
            }
        }

        public async Task<(bool Exito, string Mensaje)> ToggleActivoProfesorAsync(int idUsuario)
        {
            try
            {
                var u = await _context.Usuarios.FindAsync(idUsuario);
                if (u == null) return (false, "Usuario no encontrado.");
                u.Activo = !u.Activo;
                await _context.SaveChangesAsync();
                return (true, u.Activo ? "Profesor activado." : "Profesor desactivado.");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }


    }
}
