using AnzanMegaArithmetics.Models;
using AnzanMegaArithmetics.Models.MasterModels;
using DataBase;
using Microsoft.EntityFrameworkCore;
using System.Linq;

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

            // Totales desde la fuente real
            modelo.DashboardIzquierdo.TotalLicenciasGeneradas = await _context.Licencias_Inventario_Individual
                .CountAsync(l => l.Activo);

            modelo.DashboardIzquierdo.LicenciasAsignadas = await _context.Licencias_Inventario_Individual
                .CountAsync(l => l.Activo && l.Id_Usuario != null);

            modelo.DashboardIzquierdo.LicenciasDisponibles =
                modelo.DashboardIzquierdo.TotalLicenciasGeneradas -
                modelo.DashboardIzquierdo.LicenciasAsignadas;

            // Alertas de licencias disponibles bajas
            var listaAlertas = new List<AlertaMasterModel>();
            var institucionesActivas = await _context.Instituciones
                .Where(i => i.Activo && i.Id_Institucion != 1)
                .ToListAsync();

            foreach (var inst in institucionesActivas)
            {
                int total = await _context.Licencias_Inventario_Individual
                    .CountAsync(l => l.Id_Institucion == inst.Id_Institucion && l.Activo);

                int usadas = await _context.Licencias_Inventario_Individual
                    .CountAsync(l => l.Id_Institucion == inst.Id_Institucion
                                  && l.Activo
                                  && l.Id_Usuario != null);

                int libres = total - usadas;

                if (libres <= 10)
                {
                    listaAlertas.Add(new AlertaMasterModel
                    {
                        NombreInstitucion = inst.Nombre,
                        MensajeAlerta = libres == 0
                            ? "¡Sin licencias disponibles!"
                            : $"Solo quedan {libres} licencias",
                        TipoAlerta = libres == 0 ? "Peligro" : "Advertencia"
                    });
                }
            }

            modelo.DashboardIzquierdo.Alertas = listaAlertas;

            // Directorio de instituciones
            var institucionesBD = await _context.Instituciones.ToListAsync();
            var listaInstituciones = new List<InstitucionDirectorioModel>();

            foreach (var inst in institucionesBD)
            {
                var totalLicencias = await _context.Licencias_Inventario_Individual
                    .CountAsync(l => l.Id_Institucion == inst.Id_Institucion && l.Activo);

                var licenciasUsadas = await _context.Licencias_Inventario_Individual
                    .CountAsync(l => l.Id_Institucion == inst.Id_Institucion
                                  && l.Activo
                                  && l.Id_Usuario != null);

                listaInstituciones.Add(new InstitucionDirectorioModel
                {
                    Id_Institucion = inst.Id_Institucion,
                    Nombre = inst.Nombre,
                    Activo = inst.Activo,
                    FechaRegistro = inst.Fecha_Registro,
                    LicenciasTotales = totalLicencias,
                    LicenciasUsadas = licenciasUsadas
                });
            }

            modelo.ListaInstituciones = listaInstituciones
                .OrderBy(i => i.Id_Institucion != 1)
                .ThenBy(i => i.Nombre)
                .ToList();

            // ── NUEVAS CONSULTAS ─────────────────────────────────────

            // 1. Licencias próximas a vencer (≤60 días, con usuario asignado)
            DateTime limite60 = DateTime.Now.AddDays(60);
            var licenciasProximas = await _context.Licencias_Inventario_Individual
                .Include(l => l.Usuario).ThenInclude(u => u.Rol)
                .Include(l => l.Institucion)
                .Where(l => l.Activo
                         && l.Id_Usuario != null
                         && l.Fecha_Vencimiento <= limite60
                         && l.Fecha_Vencimiento >= DateTime.Now)
                .OrderBy(l => l.Fecha_Vencimiento)
                .Take(20)
                .ToListAsync();

            modelo.DashboardIzquierdo.LicenciasProximasAVencer = licenciasProximas
                .Select(l => new AlertaLicenciaProximaModel
                {
                    NombreUsuario = l.Usuario?.Nombre ?? "—",
                    NombreInstitucion = l.Institucion?.Nombre ?? "—",
                    FechaVencimiento = l.Fecha_Vencimiento,
                    DiasRestantes = (int)(l.Fecha_Vencimiento - DateTime.Now).TotalDays
                }).ToList();

            // 2. Usuarios activos sin licencia (roles 1, 2, 3 — excluye masters y la institución 1)
            var idsConLicencia = await _context.Licencias_Inventario_Individual
                .Where(l => l.Activo && l.Id_Usuario != null)
                .Select(l => l.Id_Usuario!.Value)
                .ToListAsync();

            var usuariosSinLicencia = await _context.Usuarios
                .Include(u => u.Rol)
                .Include(u => u.Institucion)
                .Where(u => u.Activo
                         && u.Id_Rol != 4                          // excluir masters
                         && u.Id_Institucion != 1                  // excluir institución base
                         && !idsConLicencia.Contains(u.Id_Usuario))
                .OrderBy(u => u.Institucion.Nombre)
                .ThenBy(u => u.Nombre)
                .Take(30)
                .ToListAsync();

            modelo.DashboardIzquierdo.UsuariosSinLicencia = usuariosSinLicencia
                .Select(u => new AlertaUsuarioSinLicenciaModel
                {
                    NombreUsuario = u.Nombre,
                    NombreInstitucion = u.Institucion?.Nombre ?? "—",
                    Rol = u.Rol?.Rol ?? "—"
                }).ToList();

            // 3. Clases vacías (sin ningún usuario asignado)
            var idsClasesConUsuarios = await _context.Usuarios_Clases
                .Select(uc => uc.Id_Clase)
                .Distinct()
                .ToListAsync();

            var clasesVacias = await _context.Clases
                .Include(c => c.Institucion)
                .Where(c => c.Activo
                         && c.Id_Institucion != 1
                         && !idsClasesConUsuarios.Contains(c.Id_Clase))
                .OrderBy(c => c.Institucion.Nombre)
                .ThenBy(c => c.Nombre)
                .Take(20)
                .ToListAsync();

            modelo.DashboardIzquierdo.ClasesVacias = clasesVacias
                .Select(c => new AlertaClaseVaciaModel
                {
                    NombreClase = c.Nombre,
                    NombreInstitucion = c.Institucion?.Nombre ?? "—",
                    Id_Institucion = c.Id_Institucion ?? 0
                }).ToList();

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

                var dbInstitucion = new InstitucionesDB
                {
                    Nombre = nombreLimpio,
                    Activo = true,
                    Fecha_Registro = DateTime.Now
                };

                _context.Instituciones.Add(dbInstitucion);
                await _context.SaveChangesAsync();

                var dbInventario = new Instituciones_Inventario_LicenciasDB
                {
                    Id_Institucion = dbInstitucion.Id_Institucion,
                    Id_Licencia = 1,
                    Cantidad_Total = formModel.LicenciasIniciales,
                    Cantidad_Asignada = 0
                };

                _context.Instituciones_Inventario_Licencias.Add(dbInventario);
                await _context.SaveChangesAsync();

                var licenciaDB = await _context.Licencias.FindAsync(dbInventario.Id_Licencia);
                int vigencia = licenciaDB?.Vigencia ?? 12;

                for (int i = 0; i < formModel.LicenciasIniciales; i++)
                {
                    _context.Licencias_Inventario_Individual.Add(new LicenciasInventarioIndividualDB
                    {
                        Id_Institucion = dbInstitucion.Id_Institucion,
                        Id_Licencia = dbInventario.Id_Licencia,
                        Fecha_Compra = DateTime.Now,
                        Fecha_Vencimiento = DateTime.Now.AddMonths(vigencia),
                        Id_Usuario = null,
                        Activo = true
                    });
                }
                await _context.SaveChangesAsync();

                var modulosDefault = new List<string>
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

                foreach (var clave in modulosDefault)
                {
                    _context.Instituciones_Modulos.Add(new InstitucionesModulosDB
                    {
                        Id_Institucion = dbInstitucion.Id_Institucion,
                        Clave_Modulo = clave,
                        Activo = true
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, "Institución registrada correctamente.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Error interno: {ex.Message}");
            }
        }

        public async Task<GestionLicenciasModel> ObtenerDatosLicenciasAsync(int idInstitucion)
        {
            var inst = await _context.Instituciones.FindAsync(idInstitucion);
            var inv = await _context.Instituciones_Inventario_Licencias
                .FirstOrDefaultAsync(i => i.Id_Institucion == idInstitucion);

            int totalReal = await _context.Licencias_Inventario_Individual
                .CountAsync(l => l.Id_Institucion == idInstitucion && l.Activo);

            int usadasReal = await _context.Licencias_Inventario_Individual
                .CountAsync(l => l.Id_Institucion == idInstitucion
                              && l.Activo
                              && l.Id_Usuario != null);

            return new GestionLicenciasModel
            {
                Id_Institucion = idInstitucion,
                NombreInstitucion = inst?.Nombre ?? string.Empty,
                LicenciasTotales = totalReal,
                LicenciasUsadas = usadasReal,
                NuevasLicenciasTotales = inv?.Cantidad_Total ?? totalReal
            };
        }

        public async Task<(bool Exito, string Mensaje)> ActualizarLicenciasAsync(GestionLicenciasModel model)
        {
            try
            {
                var inv = await _context.Instituciones_Inventario_Licencias
                    .FirstOrDefaultAsync(i => i.Id_Institucion == model.Id_Institucion);

                if (inv == null) return (false, "No se encontró el inventario.");

                int totalActual = inv.Cantidad_Total;
                int nuevasLicencias = model.NuevasLicenciasTotales - totalActual;

                if (model.NuevasLicenciasTotales < inv.Cantidad_Asignada)
                    return (false, $"No puedes reducir a {model.NuevasLicenciasTotales} porque ya hay {inv.Cantidad_Asignada} licencias asignadas.");

                if (nuevasLicencias > 0)
                {
                    // Obtener vigencia del tipo de licencia
                    var licenciaDB = await _context.Licencias.FindAsync(inv.Id_Licencia);
                    int vigencia = licenciaDB?.Vigencia ?? 12;

                    // Crear registros individuales para las nuevas licencias
                    for (int i = 0; i < nuevasLicencias; i++)
                    {
                        _context.Licencias_Inventario_Individual.Add(new LicenciasInventarioIndividualDB
                        {
                            Id_Institucion = model.Id_Institucion,
                            Id_Licencia = inv.Id_Licencia,
                            Fecha_Compra = DateTime.Now,
                            Fecha_Vencimiento = DateTime.Now.AddMonths(vigencia),
                            Id_Usuario = null,
                            Activo = true
                        });
                    }
                }
                else if (nuevasLicencias < 0)
                {
                    // Reducir — eliminar licencias libres (sin usuario)
                    int aEliminar = Math.Abs(nuevasLicencias);
                    var libres = await _context.Licencias_Inventario_Individual
                        .Where(l => l.Id_Institucion == model.Id_Institucion
                                 && l.Id_Usuario == null
                                 && l.Activo)
                        .OrderBy(l => l.Fecha_Vencimiento)
                        .Take(aEliminar)
                        .ToListAsync();

                    if (libres.Count < aEliminar)
                        return (false, "No hay suficientes licencias libres para reducir la cantidad.");

                    _context.Licencias_Inventario_Individual.RemoveRange(libres);
                }

                inv.Cantidad_Total = model.NuevasLicenciasTotales;
                await _context.SaveChangesAsync();
                return (true, "Inventario actualizado correctamente.");
            }
            catch (Exception ex)
            {
                return (false, $"Error al actualizar: {ex.Message}");
            }
        }

        public async Task<DetalleInstitucionViewModel> ObtenerDetalleInstitucionAsync(int id)
        {
            var inst = await _context.Instituciones.FindAsync(id);
            if (inst == null) return null;

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

            double promedioReal = 0;
            long totalPreguntasEscuela = datosPruebas.Sum(p => (long)p.Total_Preguntas);
            long totalAciertosEscuela = datosPruebas.Sum(p => (long)p.Respuestas_Correctas);

            if (totalPreguntasEscuela > 0)
                promedioReal = Math.Round((double)totalAciertosEscuela / totalPreguntasEscuela * 100, 1);

            var clasesDB = await _context.Clases
                .Where(c => c.Id_Institucion == id)
                .ToListAsync();

            var listaClasesSede = new List<ClaseSedeModel>();

            foreach (var c in clasesDB)
            {
                int alumnosEnClase = await _context.Usuarios
                    .Where(u => u.Id_Institucion == id
                             && u.Usuario_Clase.Any(uc => uc.Id_Clase == c.Id_Clase))
                    .CountAsync();

                listaClasesSede.Add(new ClaseSedeModel
                {
                    Id_Clase = c.Id_Clase,
                    Nombre = c.Nombre,
                    Activa = c.Activo,
                    CantidadAlumnos = alumnosEnClase
                });
            }

            var tiposDePrueba = await _context.Pruebas
                .Where(p => p.Usuario.Id_Institucion == id)
                .Select(p => p.Tipo_Prueba)
                .Distinct()
                .ToListAsync();

            // ↓ CORRECCIÓN: desde la fuente real
            int totalLicencias = await _context.Licencias_Inventario_Individual
                .CountAsync(l => l.Id_Institucion == id && l.Activo);

            int licenciasEnUso = await _context.Licencias_Inventario_Individual
                .CountAsync(l => l.Id_Institucion == id && l.Activo && l.Id_Usuario != null);

            return new DetalleInstitucionViewModel
            {
                Id_Institucion = inst.Id_Institucion,
                Nombre = inst.Nombre,
                FechaRegistro = inst.Fecha_Registro,
                FechaVencimiento = fechaVencimiento,
                Activa = inst.Activo,

                TotalLicencias = totalLicencias,   // ← corregido
                LicenciasEnUso = licenciasEnUso,   // ← corregido

                DiasRestantes = diasRestantes,
                PorcentajeTiempoTranscurrido = porcentajeTranscurrido,

                TotalAlumnos = totalAlumnos,
                TotalProfesores = totalProfes,
                TotalClases = totalClases,

                PromedioGeneral = promedioReal,
                LabelsActividad = labelsActividad,
                ValoresActividad = valoresActividad,

                ListaClases = listaClasesSede,
                TiposDePruebaDisponibles = tiposDePrueba
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

            // Obtener licencias individuales de todos los alumnos de una vez
            var idsAlumnos = alumnosBD.Select(u => u.Id_Usuario).ToList();
            var licenciasIndividuales = await _context.Licencias_Inventario_Individual
                .Include(l => l.Licencia)
                .Where(l => l.Id_Usuario.HasValue && idsAlumnos.Contains(l.Id_Usuario.Value) && l.Activo)
                .ToListAsync();

            var lista = alumnosBD.Select(u =>
            {
                var licInd = licenciasIndividuales.FirstOrDefault(l => l.Id_Usuario == u.Id_Usuario);

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
                    Licencia = licInd?.Licencia?.Nombre ?? "Sin licencia",
                    Fecha_Asignacion_Licencia = licInd?.Fecha_Compra,
                    Fecha_Vencimiento_Licencia = licInd?.Fecha_Vencimiento
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

            var licInd = await _context.Licencias_Inventario_Individual
    .Include(l => l.Licencia)
    .FirstOrDefaultAsync(l => l.Id_Usuario == u.Id_Usuario && l.Activo);

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
                Licencia = licInd?.Licencia?.Nombre ?? "Sin licencia",
                Fecha_Asignacion_Licencia = licInd?.Fecha_Compra,
                Fecha_Vencimiento_Licencia = licInd?.Fecha_Vencimiento
            };
        }

        public async Task<CrearAlumnoMasterModel> ObtenerFormularioCrearAlumnoAsync(int idInstitucion)
        {
            var clases = await _context.Clases
                .Where(c => c.Id_Institucion == idInstitucion && c.Activo)
                .Select(c => new ClaseSedeModel { Id_Clase = c.Id_Clase, Nombre = c.Nombre })
                .ToListAsync();

            return new CrearAlumnoMasterModel
            {
                Id_Institucion = idInstitucion,
                ClasesDisponibles = clases,
                LicenciasDisponibles = await ObtenerLicenciasDisponiblesAsync(idInstitucion)
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

                // 3. Clase válida
                bool claseValida = await _context.Clases
                    .AnyAsync(c => c.Id_Clase == model.Id_Clase
                                && c.Id_Institucion == model.Id_Institucion
                                && c.Activo);
                if (!claseValida)
                    return (false, "La clase seleccionada no pertenece a esta institución.");

                // 4. Validar licencia
                if (model.Id_Licencia_Individual <= 0)
                    return (false, "Debes seleccionar una licencia para asignar al alumno.");

                var licInd = await _context.Licencias_Inventario_Individual
                    .FindAsync(model.Id_Licencia_Individual);

                if (licInd == null)
                    return (false, "La licencia seleccionada no existe.");

                if (licInd.Id_Institucion != model.Id_Institucion)
                    return (false, "La licencia no pertenece a esta institución.");

                // Guardar si ya estaba asignada para no alterar el contador
                bool licenciaYaEstabaAsignada = licInd.Id_Usuario.HasValue;
                if (licenciaYaEstabaAsignada)
                    licInd.Id_Usuario = null;

                // 5. Crear usuario
                var nuevoUsuario = new UsuariosDB
                {
                    Id_Rol = 1,
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

                // 7. Asignar licencia
                licInd.Id_Usuario = nuevoUsuario.Id_Usuario;

                // 8. Actualizar inventario SOLO si la licencia estaba libre
                var inventario = await _context.Instituciones_Inventario_Licencias
                    .FirstOrDefaultAsync(i => i.Id_Institucion == model.Id_Institucion);
                if (inventario != null && !licenciaYaEstabaAsignada)
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
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Id_Usuario == idUsuario);

            if (u == null) return null;

            var clases = await _context.Clases
                .Where(c => c.Id_Institucion == u.Id_Institucion && c.Activo)
                .Select(c => new ClaseSedeModel { Id_Clase = c.Id_Clase, Nombre = c.Nombre })
                .ToListAsync();

            var claseActual = u.Usuario_Clase.FirstOrDefault(uc => uc.Activo);

            var licInd = await _context.Licencias_Inventario_Individual
                .Include(l => l.Licencia)
                .FirstOrDefaultAsync(l => l.Id_Usuario == idUsuario && l.Activo);

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
                LicenciaActual = licInd?.Licencia?.Nombre ?? "Sin licencia",
                Id_UsuarioLicencia = licInd?.Id,
                Fecha_Inicio_Licencia = licInd?.Fecha_Compra,
                Fecha_Fin_Licencia = licInd?.Fecha_Vencimiento,
                Id_Rol = u.Id_Rol,
                NombreRol = u.Rol?.Rol ?? "",
                LicenciasDisponibles = await ObtenerLicenciasDisponiblesAsync(u.Id_Institucion ?? 0),
                Id_Licencia_Individual = licInd?.Id ?? 0
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

                if (model.Id_Rol >= 1 && model.Id_Rol <= 4)
                    u.Id_Rol = model.Id_Rol;

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

                

                // Reasignar licencia si cambió
                if (model.Id_Licencia_Individual > 0)
                {
                    // Quitar licencia actual del usuario
                    var licActual = await _context.Licencias_Inventario_Individual
                        .FirstOrDefaultAsync(l => l.Id_Usuario == model.Id_Usuario);
                    if (licActual != null && licActual.Id != model.Id_Licencia_Individual)
                        licActual.Id_Usuario = null;

                    // Asignar la nueva
                    var licNueva = await _context.Licencias_Inventario_Individual
                        .FindAsync(model.Id_Licencia_Individual);
                    if (licNueva != null)
                        licNueva.Id_Usuario = model.Id_Usuario;
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
                .Where(u => u.Id_Institucion == idInstitucion && u.Rol.Rol == "Profesor")
                .ToListAsync();

            var idsProfes = profesoresBD.Select(u => u.Id_Usuario).ToList();
            var licenciasIndividuales = await _context.Licencias_Inventario_Individual
                .Include(l => l.Licencia)
                .Where(l => l.Id_Usuario.HasValue && idsProfes.Contains(l.Id_Usuario.Value) && l.Activo)
                .ToListAsync();

            var lista = profesoresBD.Select(u =>
            {
                var licInd = licenciasIndividuales.FirstOrDefault(l => l.Id_Usuario == u.Id_Usuario);

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
                    Licencia = licInd?.Licencia?.Nombre ?? "Sin licencia",
                    Fecha_Asignacion_Licencia = licInd?.Fecha_Compra,
                    Fecha_Vencimiento_Licencia = licInd?.Fecha_Vencimiento
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
                .Include(c => c.Institucion)
                .Where(c => c.Activo && c.Institucion.Activo)
                .OrderBy(c => c.Institucion.Nombre)
                .ThenBy(c => c.Nombre)
                .Select(c => new ClaseSedeModel
                {
                    Id_Clase = c.Id_Clase,
                    Nombre = c.Nombre,
                    Id_Institucion = c.Id_Institucion ?? 0,
                    NombreInstitucion = c.Institucion.Nombre
                })
                .ToListAsync();

            return new CrearProfesorMasterModel
            {
                Id_Institucion = idInstitucion,
                ClasesDisponibles = clases,
                LicenciasDisponibles = await ObtenerLicenciasDisponiblesAsync(idInstitucion)
            };
        }

        public async Task<(bool Exito, string Mensaje)> CrearProfesorMasterAsync(CrearProfesorMasterModel model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
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

                foreach (var idClase in model.Ids_Clases)
                {
                    bool claseValida = await _context.Clases
                        .AnyAsync(c => c.Id_Clase == idClase && c.Activo);
                    if (!claseValida)
                        return (false, "Una o más clases seleccionadas no son válidas.");
                }

                // Validar licencia
                if (model.Id_Licencia_Individual <= 0)
                    return (false, "Debes seleccionar una licencia para asignar al profesor.");

                var licInd = await _context.Licencias_Inventario_Individual
                    .FindAsync(model.Id_Licencia_Individual);

                if (licInd == null)
                    return (false, "La licencia seleccionada no existe.");

                if (licInd.Id_Institucion != model.Id_Institucion)
                    return (false, "La licencia no pertenece a esta institución.");

                bool licenciaYaEstabaAsignada = licInd.Id_Usuario.HasValue;
                if (licenciaYaEstabaAsignada)
                    licInd.Id_Usuario = null;

                // Crear profesor
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

                // Asignar clases
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
                licInd.Id_Usuario = nuevoProfesor.Id_Usuario;

                // Actualizar inventario SOLO si la licencia estaba libre
                var inventario = await _context.Instituciones_Inventario_Licencias
                    .FirstOrDefaultAsync(i => i.Id_Institucion == model.Id_Institucion);
                if (inventario != null && !licenciaYaEstabaAsignada)
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
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Id_Usuario == idUsuario);

            if (u == null) return null;

            var clases = await _context.Clases
                .Include(c => c.Institucion)
                .Where(c => c.Activo && c.Institucion.Activo)
                .OrderBy(c => c.Institucion.Nombre)
                .ThenBy(c => c.Nombre)
                .Select(c => new ClaseSedeModel
                {
                    Id_Clase = c.Id_Clase,
                    Nombre = c.Nombre,
                    Id_Institucion = c.Id_Institucion ?? 0,
                    NombreInstitucion = c.Institucion.Nombre
                })
                .ToListAsync();

            var licInd = await _context.Licencias_Inventario_Individual
                .Include(l => l.Licencia)
                .FirstOrDefaultAsync(l => l.Id_Usuario == idUsuario && l.Activo);

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
                LicenciaActual = licInd?.Licencia?.Nombre ?? "Sin licencia",
                Id_UsuarioLicencia = licInd?.Id,
                Fecha_Inicio_Licencia = licInd?.Fecha_Compra,
                Fecha_Fin_Licencia = licInd?.Fecha_Vencimiento,
                Id_Rol = u.Id_Rol,
                NombreRol = u.Rol?.Rol ?? "",
                LicenciasDisponibles = await ObtenerLicenciasDisponiblesAsync(u.Id_Institucion ?? 0),
                Id_Licencia_Individual = licInd?.Id ?? 0
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
                        .AnyAsync(c => c.Id_Clase == idClase && c.Activo);
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

                if (model.Id_Rol >= 1 && model.Id_Rol <= 4)
                    u.Id_Rol = model.Id_Rol;

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

                
                

                // Reasignar licencia si cambió
                if (model.Id_Licencia_Individual > 0)
                {
                    // Quitar licencia actual del usuario
                    var licActual = await _context.Licencias_Inventario_Individual
                        .FirstOrDefaultAsync(l => l.Id_Usuario == model.Id_Usuario);
                    if (licActual != null && licActual.Id != model.Id_Licencia_Individual)
                        licActual.Id_Usuario = null;

                    // Asignar la nueva
                    var licNueva = await _context.Licencias_Inventario_Individual
                        .FindAsync(model.Id_Licencia_Individual);
                    if (licNueva != null)
                        licNueva.Id_Usuario = model.Id_Usuario;
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


        public async Task<ListadoAdminsViewModel> ObtenerAdminsInstitucionAsync(int idInstitucion)
        {
            var adminsBD = await _context.Usuarios
                .Include(u => u.Rol)
                .Include(u => u.Usuario_Clase).ThenInclude(uc => uc.Clase)
                .Where(u => u.Id_Institucion == idInstitucion &&
                           (u.Id_Rol == 3 || u.Id_Rol == 4))
                .ToListAsync();

            var idsAdmins = adminsBD.Select(u => u.Id_Usuario).ToList();
            var licenciasIndividuales = await _context.Licencias_Inventario_Individual
                .Include(l => l.Licencia)
                .Where(l => l.Id_Usuario.HasValue && idsAdmins.Contains(l.Id_Usuario.Value) && l.Activo)
                .ToListAsync();

            var lista = adminsBD.Select(u =>
            {
                var licInd = licenciasIndividuales.FirstOrDefault(l => l.Id_Usuario == u.Id_Usuario);

                return new UsuarioBDModel
                {
                    Id_Usuario = u.Id_Usuario,
                    Id_Rol = u.Id_Rol.ToString(),
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
                                                .Select(uc => uc.Clase.Nombre)
                                                .ToList(),
                    Licencia = licInd?.Licencia?.Nombre ?? "Sin licencia",
                    Fecha_Asignacion_Licencia = licInd?.Fecha_Compra,
                    Fecha_Vencimiento_Licencia = licInd?.Fecha_Vencimiento
                };
            }).ToList();

            return new ListadoAdminsViewModel
            {
                Id_Institucion = idInstitucion,
                ListaAdmins = lista
            };
        }

        public async Task<CrearAdminMasterModel> ObtenerFormularioCrearAdminAsync(int idInstitucion)
        {
            var clases = await _context.Clases
                .Include(c => c.Institucion)
                .Where(c => c.Activo && c.Institucion.Activo)
                .OrderBy(c => c.Institucion.Nombre)
                .ThenBy(c => c.Nombre)
                .Select(c => new ClaseSedeModel
                {
                    Id_Clase = c.Id_Clase,
                    Nombre = c.Nombre,
                    Id_Institucion = c.Id_Institucion ?? 0,
                    NombreInstitucion = c.Institucion.Nombre
                })
                .ToListAsync();

            return new CrearAdminMasterModel
            {
                Id_Institucion = idInstitucion,
                ClasesDisponibles = clases,
                LicenciasDisponibles = await ObtenerLicenciasDisponiblesAsync(idInstitucion),
                Id_Rol = 3
            };
        }

        public async Task<(bool Exito, string Mensaje)> CrearAdminMasterAsync(CrearAdminMasterModel model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (model.Id_Rol < 1 || model.Id_Rol > 4)
                    return (false, "Rol inválido.");

                bool correoExiste = await _context.Usuarios
                    .AnyAsync(u => u.Correo.ToLower() == model.Correo.Trim().ToLower());
                if (correoExiste)
                    return (false, "Ese correo ya está registrado en la plataforma.");

                bool gtExiste = await _context.Usuarios
                    .AnyAsync(u => u.Gamer_Tag.ToLower() == model.Gamer_Tag.Trim().ToLower());
                if (gtExiste)
                    return (false, "Ese GamerTag ya está en uso.");

                // Validar licencia
                if (model.Id_Licencia_Individual <= 0)
                    return (false, "Debes seleccionar una licencia para asignar al usuario.");

                var licInd = await _context.Licencias_Inventario_Individual
                    .FindAsync(model.Id_Licencia_Individual);

                if (licInd == null)
                    return (false, "La licencia seleccionada no existe.");

                if (licInd.Id_Institucion != model.Id_Institucion)
                    return (false, "La licencia no pertenece a esta institución.");

                bool licenciaYaEstabaAsignada = licInd.Id_Usuario.HasValue;
                if (licenciaYaEstabaAsignada)
                    licInd.Id_Usuario = null;

                // Crear usuario
                var nuevo = new UsuariosDB
                {
                    Id_Rol = model.Id_Rol,
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

                _context.Usuarios.Add(nuevo);
                await _context.SaveChangesAsync();

                // Clases (opcionales para admins)
                if (model.Ids_Clases != null)
                {
                    foreach (var idClase in model.Ids_Clases)
                    {
                        bool claseValida = await _context.Clases
                            .AnyAsync(c => c.Id_Clase == idClase && c.Activo);
                        if (claseValida)
                        {
                            _context.Usuarios_Clases.Add(new Usuario_ClaseDB
                            {
                                Id_Usuario = nuevo.Id_Usuario,
                                Id_Clase = idClase,
                                Activo = true
                            });
                        }
                    }
                }

                // Asignar licencia
                licInd.Id_Usuario = nuevo.Id_Usuario;

                // Actualizar inventario SOLO si la licencia estaba libre
                var inventario = await _context.Instituciones_Inventario_Licencias
                    .FirstOrDefaultAsync(i => i.Id_Institucion == model.Id_Institucion);
                if (inventario != null && !licenciaYaEstabaAsignada)
                    inventario.Cantidad_Asignada++;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, $"Usuario '{nuevo.Nombre}' creado correctamente.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Error interno: {ex.Message}");
            }
        }

        public async Task<EditarAdminMasterModel> ObtenerDatosEditarAdminMasterAsync(int idUsuario)
        {
            var u = await _context.Usuarios
                .Include(u => u.Rol)
                .Include(u => u.Usuario_Clase).ThenInclude(uc => uc.Clase)
                .FirstOrDefaultAsync(u => u.Id_Usuario == idUsuario);

            if (u == null) return null;

            var clases = await _context.Clases
                .Include(c => c.Institucion)
                .Where(c => c.Activo && c.Institucion.Activo)
                .OrderBy(c => c.Institucion.Nombre)
                .ThenBy(c => c.Nombre)
                .Select(c => new ClaseSedeModel
                {
                    Id_Clase = c.Id_Clase,
                    Nombre = c.Nombre,
                    Id_Institucion = c.Id_Institucion ?? 0,
                    NombreInstitucion = c.Institucion.Nombre
                })
                .ToListAsync();

            var licInd = await _context.Licencias_Inventario_Individual
                .Include(l => l.Licencia)
                .FirstOrDefaultAsync(l => l.Id_Usuario == idUsuario && l.Activo);

            return new EditarAdminMasterModel
            {
                Id_Usuario = u.Id_Usuario,
                Nombre = u.Nombre,
                Gamer_Tag = u.Gamer_Tag,
                Correo = u.Correo,
                Pass = u.Pass,
                Racha = u.Racha,
                Exp = u.Experiencia_Total,
                Activo = u.Activo,
                Id_Rol = u.Id_Rol,
                NombreRol = u.Rol?.Rol ?? "",
                Ids_Clases_Actuales = u.Usuario_Clase.Select(uc => uc.Id_Clase).ToList(),
                Ids_Clases_Nuevas = u.Usuario_Clase.Select(uc => uc.Id_Clase).ToList(),
                ClasesDisponibles = clases,
                LicenciaActual = licInd?.Licencia?.Nombre ?? "Sin licencia",
                Id_UsuarioLicencia = licInd?.Id,
                Fecha_Inicio_Licencia = licInd?.Fecha_Compra,
                Fecha_Fin_Licencia = licInd?.Fecha_Vencimiento,
                LicenciasDisponibles = await ObtenerLicenciasDisponiblesAsync(u.Id_Institucion ?? 0),
                Id_Licencia_Individual = licInd?.Id ?? 0
            };
        }

        public async Task<(bool Exito, string Mensaje)> GuardarEdicionAdminMasterAsync(EditarAdminMasterModel model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var u = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.Id_Usuario == model.Id_Usuario);

                if (u == null) return (false, "Usuario no encontrado.");

                if (model.Id_Rol < 1 || model.Id_Rol > 4)
                    return (false, "Rol inválido.");

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
                u.Id_Rol = model.Id_Rol;

                // Reasignar clases
                var registrosActuales = await _context.Usuarios_Clases
                    .Where(uc => uc.Id_Usuario == model.Id_Usuario)
                    .ToListAsync();

                var idsNuevos = model.Ids_Clases_Nuevas ?? new List<int>();

                var aEliminar = registrosActuales
                    .Where(uc => !idsNuevos.Contains(uc.Id_Clase))
                    .ToList();
                _context.Usuarios_Clases.RemoveRange(aEliminar);

                var idsExistentes = registrosActuales.Select(uc => uc.Id_Clase).ToList();
                foreach (var idClase in idsNuevos)
                {
                    if (!idsExistentes.Contains(idClase))
                        _context.Usuarios_Clases.Add(new Usuario_ClaseDB
                        {
                            Id_Usuario = model.Id_Usuario,
                            Id_Clase = idClase,
                            Activo = true
                        });
                }

               
               

                // Reasignar licencia si cambió
                if (model.Id_Licencia_Individual > 0)
                {
                    // Quitar licencia actual del usuario
                    var licActual = await _context.Licencias_Inventario_Individual
                        .FirstOrDefaultAsync(l => l.Id_Usuario == model.Id_Usuario);
                    if (licActual != null && licActual.Id != model.Id_Licencia_Individual)
                        licActual.Id_Usuario = null;

                    // Asignar la nueva
                    var licNueva = await _context.Licencias_Inventario_Individual
                        .FindAsync(model.Id_Licencia_Individual);
                    if (licNueva != null)
                        licNueva.Id_Usuario = model.Id_Usuario;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return (true, "Usuario actualizado correctamente.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Error interno: {ex.Message}");
            }
        }

        public async Task<(bool Exito, string Mensaje)> ToggleActivoAdminAsync(int idUsuario)
        {
            try
            {
                var u = await _context.Usuarios.FindAsync(idUsuario);
                if (u == null) return (false, "Usuario no encontrado.");
                u.Activo = !u.Activo;
                await _context.SaveChangesAsync();
                return (true, u.Activo ? "Usuario activado." : "Usuario desactivado.");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        public async Task<(bool Exito, string Mensaje)> EliminarUsuarioAsync(int idUsuario)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var usuario = await _context.Usuarios.FindAsync(idUsuario);
                if (usuario == null)
                    return (false, "Usuario no encontrado.");

                var inventario = usuario.Id_Institucion.HasValue
                    ? await _context.Instituciones_Inventario_Licencias
                        .FirstOrDefaultAsync(i => i.Id_Institucion == usuario.Id_Institucion.Value)
                    : null;

                // 1. Pruebas
                var pruebas = await _context.Pruebas
                    .Where(p => p.Id_Usuario == idUsuario)
                    .ToListAsync();
                _context.Pruebas.RemoveRange(pruebas);

                // 2. Historial de licencias (Usuarios_Licencias) — solo eliminar, sin tocar contadores
                var licenciasHistorial = await _context.Usuarios_Licencias
                    .Where(ul => ul.Id_Usuario == idUsuario)
                    .ToListAsync();
                _context.Usuarios_Licencias.RemoveRange(licenciasHistorial);

                // 3. Clases
                var clases = await _context.Usuarios_Clases
                    .Where(uc => uc.Id_Usuario == idUsuario)
                    .ToListAsync();
                _context.Usuarios_Clases.RemoveRange(clases);

                // 4. Licencia individual — aquí sí descontamos UNA VEZ
                var licInd = await _context.Licencias_Inventario_Individual
                    .FirstOrDefaultAsync(l => l.Id_Usuario == idUsuario);
                if (licInd != null)
                {
                    licInd.Id_Usuario = null;
                    if (inventario != null)
                        inventario.Cantidad_Asignada = Math.Max(0, inventario.Cantidad_Asignada - 1);
                }

                // 5. Eliminar usuario
                _context.Usuarios.Remove(usuario);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, "Usuario eliminado correctamente.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Error interno: {ex.Message}");
            }
        }

        public async Task<EliminarClaseModel?> ObtenerFormularioEliminarClaseAsync(int idClase)
        {
            var clase = await _context.Clases.FindAsync(idClase);
            if (clase == null) return null;

            // Clases disponibles de la misma institución, excluyendo la que se va a eliminar
            var clasesDestino = await _context.Clases
                .Where(c => c.Id_Institucion == clase.Id_Institucion
                         && c.Id_Clase != idClase
                         && c.Activo)
                .Select(c => new ClaseSedeModel { Id_Clase = c.Id_Clase, Nombre = c.Nombre })
                .ToListAsync();

            // Contar usuarios únicos asignados a esta clase
            int cantidadUsuarios = await _context.Usuarios_Clases
                .Where(uc => uc.Id_Clase == idClase)
                .Select(uc => uc.Id_Usuario)
                .Distinct()
                .CountAsync();

            return new EliminarClaseModel
            {
                Id_Clase = idClase,
                NombreClase = clase.Nombre,
                CantidadUsuarios = cantidadUsuarios,
                ClasesDisponibles = clasesDestino
            };
        }

        public async Task<(bool Exito, string Mensaje)> EliminarClaseAsync(EliminarClaseModel model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var clase = await _context.Clases.FindAsync(model.Id_Clase);
                if (clase == null)
                    return (false, "La clase no existe.");

                // Si hay usuarios, reasignarlos
                var registrosClase = await _context.Usuarios_Clases
                    .Where(uc => uc.Id_Clase == model.Id_Clase)
                    .ToListAsync();

                if (registrosClase.Any())
                {
                    if (model.Id_Clase_Destino == 0)
                        return (false, "Debes seleccionar una clase destino para los usuarios.");

                    // Verificar que la clase destino existe y es de la misma institución
                    bool destinoValido = await _context.Clases
                        .AnyAsync(c => c.Id_Clase == model.Id_Clase_Destino
                                    && c.Id_Institucion == clase.Id_Institucion
                                    && c.Activo);
                    if (!destinoValido)
                        return (false, "La clase destino no es válida.");

                    foreach (var registro in registrosClase)
                    {
                        // Verificar si el usuario ya tiene un registro en la clase destino
                        bool yaExiste = await _context.Usuarios_Clases
                            .AnyAsync(uc => uc.Id_Usuario == registro.Id_Usuario
                                         && uc.Id_Clase == model.Id_Clase_Destino);

                        if (!yaExiste)
                        {
                            _context.Usuarios_Clases.Add(new Usuario_ClaseDB
                            {
                                Id_Usuario = registro.Id_Usuario,
                                Id_Clase = model.Id_Clase_Destino,
                                Activo = true
                            });
                        }
                    }

                    // Eliminar todos los registros de la clase a borrar
                    _context.Usuarios_Clases.RemoveRange(registrosClase);
                }

                // Eliminar la clase
                _context.Clases.Remove(clase);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, "Clase eliminada y usuarios reasignados correctamente.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Error interno: {ex.Message}");
            }
        }

        private async Task<List<InstitucionOpcionModel>> ObtenerInstitucionesConLicencias(int excluirId)
        {
            var instituciones = await _context.Instituciones
                .Where(i => i.Activo && i.Id_Institucion != excluirId)
                .ToListAsync();

            var lista = new List<InstitucionOpcionModel>();

            foreach (var inst in instituciones)
            {
                int libres = await _context.Licencias_Inventario_Individual
                    .CountAsync(l => l.Id_Institucion == inst.Id_Institucion
                                  && l.Activo
                                  && l.Id_Usuario == null);

                lista.Add(new InstitucionOpcionModel
                {
                    Id_Institucion = inst.Id_Institucion,
                    Nombre = inst.Nombre,
                    LicenciasDisponibles = libres
                });
            }

            return lista;
        }

        public async Task<List<ClaseSedeModel>> ObtenerClasesPorInstitucionAsync(int idInstitucion)
        {
            return await _context.Clases
                .Where(c => c.Id_Institucion == idInstitucion && c.Activo)
                .Select(c => new ClaseSedeModel { Id_Clase = c.Id_Clase, Nombre = c.Nombre })
                .ToListAsync();
        }

        public async Task<MoverUsuarioModel?> ObtenerFormularioMoverUsuarioAsync(int idUsuario)
        {
            var u = await _context.Usuarios
                .Include(u => u.Rol)
                .Include(u => u.Institucion)
                .FirstOrDefaultAsync(u => u.Id_Usuario == idUsuario);

            if (u == null) return null;

            var instituciones = await ObtenerInstitucionesConLicencias(u.Id_Institucion ?? 0);

            return new MoverUsuarioModel
            {
                Id_Usuario = u.Id_Usuario,
                NombreUsuario = u.Nombre,
                RolUsuario = u.Rol?.Rol ?? "",
                Id_Institucion_Origen = u.Id_Institucion ?? 0,
                NombreInstitucionOrigen = u.Institucion?.Nombre ?? "",
                InstitucionesDisponibles = instituciones
            };
        }

        public async Task<(bool Exito, string Mensaje)> MoverUsuarioAsync(MoverUsuarioModel model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var usuario = await _context.Usuarios.FindAsync(model.Id_Usuario);
                if (usuario == null) return (false, "Usuario no encontrado.");

                // Verificar que la clase destino pertenece a la institución destino
                bool claseValida = await _context.Clases
                    .AnyAsync(c => c.Id_Clase == model.Id_Clase_Destino
                                && c.Id_Institucion == model.Id_Institucion_Destino
                                && c.Activo);
                if (!claseValida)
                    return (false, "La clase destino no es válida.");

                // Verificar licencia seleccionada en destino desde la fuente real
                if (model.Id_Licencia_Individual <= 0)
                    return (false, "Debes seleccionar una licencia en la institución destino.");

                var licDestino = await _context.Licencias_Inventario_Individual
                    .FindAsync(model.Id_Licencia_Individual);

                if (licDestino == null)
                    return (false, "La licencia seleccionada no existe.");

                if (licDestino.Id_Usuario.HasValue)
                    return (false, "La licencia seleccionada ya está asignada a otro usuario.");

                if (licDestino.Id_Institucion != model.Id_Institucion_Destino)
                    return (false, "La licencia no pertenece a la institución destino.");

                // Inventarios
                var invOrigen = await _context.Instituciones_Inventario_Licencias
                    .FirstOrDefaultAsync(i => i.Id_Institucion == usuario.Id_Institucion);

                var invDestino = await _context.Instituciones_Inventario_Licencias
                    .FirstOrDefaultAsync(i => i.Id_Institucion == model.Id_Institucion_Destino);

                // Desasignar licencia en origen
                var licOrigen = await _context.Licencias_Inventario_Individual
                    .FirstOrDefaultAsync(l => l.Id_Usuario == model.Id_Usuario);
                if (licOrigen != null)
                {
                    licOrigen.Id_Usuario = null;
                    if (invOrigen != null)
                        invOrigen.Cantidad_Asignada = Math.Max(0, invOrigen.Cantidad_Asignada - 1);
                }

                // Asignar licencia en destino
                licDestino.Id_Usuario = model.Id_Usuario;
                if (invDestino != null)
                    invDestino.Cantidad_Asignada++;

                // Cambiar institución
                usuario.Id_Institucion = model.Id_Institucion_Destino;

                // Reasignar clase
                var clasesActuales = await _context.Usuarios_Clases
                    .Where(uc => uc.Id_Usuario == model.Id_Usuario)
                    .ToListAsync();
                _context.Usuarios_Clases.RemoveRange(clasesActuales);

                _context.Usuarios_Clases.Add(new Usuario_ClaseDB
                {
                    Id_Usuario = model.Id_Usuario,
                    Id_Clase = model.Id_Clase_Destino,
                    Activo = true
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, $"Usuario '{usuario.Nombre}' movido correctamente.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Error interno: {ex.Message}");
            }
        }

        public async Task<MoverClaseModel?> ObtenerFormularioMoverClaseAsync(int idClase)
        {
            var clase = await _context.Clases
                .Include(c => c.Institucion)
                .FirstOrDefaultAsync(c => c.Id_Clase == idClase);

            if (clase == null) return null;

            int cantidadUsuarios = await _context.Usuarios_Clases
                .Where(uc => uc.Id_Clase == idClase)
                .Select(uc => uc.Id_Usuario)
                .Distinct()
                .CountAsync();

            var instituciones = await ObtenerInstitucionesConLicencias(clase.Id_Institucion ?? 0);

            return new MoverClaseModel
            {
                Id_Clase = idClase,
                NombreClase = clase.Nombre,
                CantidadUsuarios = cantidadUsuarios,
                Id_Institucion_Origen = clase.Id_Institucion ?? 0,
                NombreInstitucionOrigen = clase.Institucion?.Nombre ?? "",
                InstitucionesDisponibles = instituciones
            };
        }

        public async Task<(bool Exito, string Mensaje)> MoverClaseAsync(MoverClaseModel model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var clase = await _context.Clases.FindAsync(model.Id_Clase);
                if (clase == null) return (false, "Clase no encontrada.");

                var usuariosIds = await _context.Usuarios_Clases
                    .Where(uc => uc.Id_Clase == model.Id_Clase)
                    .Select(uc => uc.Id_Usuario)
                    .Distinct()
                    .ToListAsync();

                var invDestino = await _context.Instituciones_Inventario_Licencias
                    .FirstOrDefaultAsync(i => i.Id_Institucion == model.Id_Institucion_Destino);

                if (invDestino == null)
                    return (false, "La institución destino no tiene inventario de licencias.");

                // Verificar licencias disponibles en destino desde la fuente real
                int licenciasDisponibles = await _context.Licencias_Inventario_Individual
                    .CountAsync(l => l.Id_Institucion == model.Id_Institucion_Destino
                                  && l.Activo
                                  && l.Id_Usuario == null);

                if (usuariosIds.Count > licenciasDisponibles)
                    return (false, $"La institución destino solo tiene {licenciasDisponibles} licencias " +
                                   $"disponibles y se necesitan {usuariosIds.Count}.");

                // Crear o reutilizar clase en destino
                bool claseYaExiste = await _context.Clases
                    .AnyAsync(c => c.Id_Institucion == model.Id_Institucion_Destino
                                && c.Nombre.ToLower() == clase.Nombre.ToLower());

                ClaseDB claseDestino;
                if (claseYaExiste)
                {
                    claseDestino = await _context.Clases
                        .FirstAsync(c => c.Id_Institucion == model.Id_Institucion_Destino
                                      && c.Nombre.ToLower() == clase.Nombre.ToLower());
                }
                else
                {
                    claseDestino = new ClaseDB
                    {
                        Nombre = clase.Nombre,
                        Id_Institucion = model.Id_Institucion_Destino,
                        Activo = true
                    };
                    _context.Clases.Add(claseDestino);
                    await _context.SaveChangesAsync();
                }

                var invOrigen = await _context.Instituciones_Inventario_Licencias
                    .FirstOrDefaultAsync(i => i.Id_Institucion == clase.Id_Institucion);

                // Obtener licencias libres del destino para asignarlas
                var licenciasLibresDestino = await _context.Licencias_Inventario_Individual
                    .Where(l => l.Id_Institucion == model.Id_Institucion_Destino
                             && l.Activo
                             && l.Id_Usuario == null)
                    .OrderBy(l => l.Fecha_Vencimiento)
                    .Take(usuariosIds.Count)
                    .ToListAsync();

                int indexLicencia = 0;

                foreach (var idUsuario in usuariosIds)
                {
                    var usuario = await _context.Usuarios.FindAsync(idUsuario);
                    if (usuario == null) continue;

                    // Desasignar licencia individual en origen
                    var licOrigen = await _context.Licencias_Inventario_Individual
                        .FirstOrDefaultAsync(l => l.Id_Usuario == idUsuario);
                    if (licOrigen != null)
                    {
                        licOrigen.Id_Usuario = null;
                        if (invOrigen != null)
                            invOrigen.Cantidad_Asignada = Math.Max(0, invOrigen.Cantidad_Asignada - 1);
                    }

                    // Asignar licencia libre en destino
                    if (indexLicencia < licenciasLibresDestino.Count)
                    {
                        var licDestino = licenciasLibresDestino[indexLicencia];
                        licDestino.Id_Usuario = idUsuario;
                        invDestino.Cantidad_Asignada++;
                        indexLicencia++;
                    }

                    // Cambiar institución del usuario
                    usuario.Id_Institucion = model.Id_Institucion_Destino;

                    // Reasignar clase
                    var clasesActuales = await _context.Usuarios_Clases
                        .Where(uc => uc.Id_Usuario == idUsuario)
                        .ToListAsync();
                    _context.Usuarios_Clases.RemoveRange(clasesActuales);

                    _context.Usuarios_Clases.Add(new Usuario_ClaseDB
                    {
                        Id_Usuario = idUsuario,
                        Id_Clase = claseDestino.Id_Clase,
                        Activo = true
                    });
                }

                // Eliminar clase origen
                _context.Clases.Remove(clase);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, $"Clase '{clase.Nombre}' y {usuariosIds.Count} usuario(s) movidos correctamente.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Error interno: {ex.Message}");
            }
        }

        public async Task<(bool Exito, string Mensaje)> EliminarInstitucionAsync(int idInstitucion)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var institucion = await _context.Instituciones.FindAsync(idInstitucion);
                if (institucion == null)
                    return (false, "Institución no encontrada.");

                var usuarios = await _context.Usuarios
                    .Where(u => u.Id_Institucion == idInstitucion)
                    .ToListAsync();

                var idsUsuarios = usuarios.Select(u => u.Id_Usuario).ToList();

                // 1. Pruebas
                var pruebas = await _context.Pruebas
                    .Where(p => idsUsuarios.Contains(p.Id_Usuario))
                    .ToListAsync();
                _context.Pruebas.RemoveRange(pruebas);

                // 2. Usuarios_Licencias (historial)
                var licenciasHistorial = await _context.Usuarios_Licencias
                    .Where(ul => idsUsuarios.Contains(ul.Id_Usuario))
                    .ToListAsync();
                _context.Usuarios_Licencias.RemoveRange(licenciasHistorial);

                // 3. Usuarios_Clases
                var clasesUsuarios = await _context.Usuarios_Clases
                    .Where(uc => idsUsuarios.Contains(uc.Id_Usuario))
                    .ToListAsync();
                _context.Usuarios_Clases.RemoveRange(clasesUsuarios);

                // 4. Licencias individuales — ANTES de eliminar usuarios
                //    para evitar conflictos de FK con Id_Usuario
                var licenciasIndividuales = await _context.Licencias_Inventario_Individual
                    .Where(l => l.Id_Institucion == idInstitucion)
                    .ToListAsync();
                _context.Licencias_Inventario_Individual.RemoveRange(licenciasIndividuales);

                // 5. Usuarios
                _context.Usuarios.RemoveRange(usuarios);

                // 6. Clases
                var clases = await _context.Clases
                    .Where(c => c.Id_Institucion == idInstitucion)
                    .ToListAsync();
                _context.Clases.RemoveRange(clases);

                // 7. Módulos
                var modulos = await _context.Instituciones_Modulos
                    .Where(m => m.Id_Institucion == idInstitucion)
                    .ToListAsync();
                _context.Instituciones_Modulos.RemoveRange(modulos);

                // 8. Inventario de licencias
                var inventario = await _context.Instituciones_Inventario_Licencias
                    .Where(i => i.Id_Institucion == idInstitucion)
                    .ToListAsync();
                _context.Instituciones_Inventario_Licencias.RemoveRange(inventario);

                // 9. Institución
                _context.Instituciones.Remove(institucion);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, $"Institución '{institucion.Nombre}' eliminada correctamente.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Error interno: {ex.Message}");
            }
        }

        public async Task<(bool Exito, string Mensaje)> ToggleActivoInstitucionAsync(int idInstitucion)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var institucion = await _context.Instituciones.FindAsync(idInstitucion);
                if (institucion == null)
                    return (false, "Institución no encontrada.");

                bool nuevoEstado = !institucion.Activo;
                institucion.Activo = nuevoEstado;

                // Activar/desactivar todos los usuarios de la institución
                var usuarios = await _context.Usuarios
                    .Where(u => u.Id_Institucion == idInstitucion)
                    .ToListAsync();

                foreach (var u in usuarios)
                    u.Activo = nuevoEstado;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                string accion = nuevoEstado ? "activada" : "desactivada";
                return (true, $"Institución {accion}. {usuarios.Count} usuarios afectados.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Error interno: {ex.Message}");
            }
        }

        public async Task<ConfiguracionInstitucionViewModel> ObtenerConfiguracionModulosAsync(int idInstitucion)
        {
            var inst = await _context.Instituciones.FindAsync(idInstitucion);

            var modulosActivos = await _context.Instituciones_Modulos
                .Where(m => m.Id_Institucion == idInstitucion && m.Activo)
                .Select(m => m.Clave_Modulo)
                .ToHashSetAsync();

            return new ConfiguracionInstitucionViewModel
            {
                Id_Institucion = idInstitucion,
                NombreInstitucion = inst?.Nombre ?? "",
                ModulosActivos = modulosActivos
            };
        }

        public async Task<(bool Exito, string Mensaje)> GuardarConfiguracionModulosAsync(
            int idInstitucion, List<string> modulosActivos)
        {
            try
            {
                var registros = await _context.Instituciones_Modulos
                    .Where(m => m.Id_Institucion == idInstitucion)
                    .ToListAsync();

                foreach (var registro in registros)
                    registro.Activo = modulosActivos.Contains(registro.Clave_Modulo);

                await _context.SaveChangesAsync();
                return (true, "Configuración guardada correctamente.");
            }
            catch (Exception ex)
            {
                return (false, $"Error interno: {ex.Message}");
            }
        }

        public async Task<List<LicenciaDisponibleModel>> ObtenerLicenciasDisponiblesAsync(int idInstitucion)
        {
            var licencias = await _context.Licencias_Inventario_Individual
                .Include(l => l.Licencia)
                .Include(l => l.Usuario)
                .Where(l => l.Id_Institucion == idInstitucion && l.Activo)
                .OrderBy(l => l.Id_Usuario.HasValue) // libres primero
                .ThenBy(l => l.Fecha_Vencimiento)
                .Select(l => new LicenciaDisponibleModel
                {
                    Id = l.Id,
                    TipoLicencia = l.Licencia.Nombre,
                    Fecha_Compra = l.Fecha_Compra,
                    Fecha_Vencimiento = l.Fecha_Vencimiento,
                    Libre = l.Id_Usuario == null,
                    Id_Usuario_Actual = l.Id_Usuario,
                    NombreUsuarioActual = l.Usuario != null ? l.Usuario.Nombre : string.Empty
                })
                .ToListAsync();

            return licencias;
        }

        public async Task<(bool Exito, string Mensaje)> AsignarLicenciaIndividualAsync(
    int idLicenciaIndividual, int idUsuario)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var licencia = await _context.Licencias_Inventario_Individual
                    .FindAsync(idLicenciaIndividual);
                if (licencia == null)
                    return (false, "Licencia no encontrada.");

                // Si la licencia ya tiene otro usuario, desasignarlo primero
                if (licencia.Id_Usuario.HasValue && licencia.Id_Usuario != idUsuario)
                {
                    // Actualizar inventario del usuario anterior
                    var usuarioAnterior = await _context.Usuarios
                        .FindAsync(licencia.Id_Usuario.Value);

                    if (usuarioAnterior?.Id_Institucion.HasValue == true)
                    {
                        var invOrigen = await _context.Instituciones_Inventario_Licencias
                            .FirstOrDefaultAsync(i => i.Id_Institucion == usuarioAnterior.Id_Institucion);
                        if (invOrigen != null)
                            invOrigen.Cantidad_Asignada = Math.Max(0, invOrigen.Cantidad_Asignada - 1);
                    }
                }

                // Desasignar licencia anterior del usuario destino si tiene una
                await DesasignarLicenciaIndividualAsync(idUsuario);

                // Asignar la nueva licencia
                licencia.Id_Usuario = idUsuario;

                // Actualizar inventario de la institución destino
                var usuario = await _context.Usuarios.FindAsync(idUsuario);
                if (usuario?.Id_Institucion.HasValue == true)
                {
                    var invDestino = await _context.Instituciones_Inventario_Licencias
                        .FirstOrDefaultAsync(i => i.Id_Institucion == usuario.Id_Institucion);
                    if (invDestino != null)
                        invDestino.Cantidad_Asignada++;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return (true, "Licencia asignada correctamente.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Error interno: {ex.Message}");
            }
        }

        public async Task<(bool Exito, string Mensaje)> DesasignarLicenciaIndividualAsync(int idUsuario)
        {
            try
            {
                var licenciaActual = await _context.Licencias_Inventario_Individual
                    .FirstOrDefaultAsync(l => l.Id_Usuario == idUsuario);

                if (licenciaActual == null) return (true, "Sin licencia previa.");

                licenciaActual.Id_Usuario = null;

                // Actualizar inventario
                var usuario = await _context.Usuarios.FindAsync(idUsuario);
                if (usuario?.Id_Institucion.HasValue == true)
                {
                    var inv = await _context.Instituciones_Inventario_Licencias
                        .FirstOrDefaultAsync(i => i.Id_Institucion == usuario.Id_Institucion);
                    if (inv != null)
                        inv.Cantidad_Asignada = Math.Max(0, inv.Cantidad_Asignada - 1);
                }

                await _context.SaveChangesAsync();
                return (true, "Licencia desasignada.");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        public async Task<GestionLicenciasIndividualViewModel> ObtenerTabLicenciasAsync(int idInstitucion)
        {
            var inst = await _context.Instituciones.FindAsync(idInstitucion);

            var licencias = await _context.Licencias_Inventario_Individual
                .Include(l => l.Licencia)
                .Include(l => l.Usuario)
                .Where(l => l.Id_Institucion == idInstitucion)
                .OrderBy(l => l.Id_Usuario.HasValue)
                .ThenBy(l => l.Fecha_Vencimiento)
                .ToListAsync();

            var lista = licencias.Select(l => new LicenciaIndividualDetalleModel
            {
                Id = l.Id,
                TipoLicencia = l.Licencia?.Nombre ?? "—",
                Fecha_Compra = l.Fecha_Compra,
                Fecha_Vencimiento = l.Fecha_Vencimiento,
                Activo = l.Activo,
                Id_Usuario = l.Id_Usuario,
                NombreUsuario = l.Usuario?.Nombre ?? string.Empty,
                CorreoUsuario = l.Usuario?.Correo ?? string.Empty
            }).ToList();

            return new GestionLicenciasIndividualViewModel
            {
                Id_Institucion = idInstitucion,
                NombreInstitucion = inst?.Nombre ?? string.Empty,
                TotalLicencias = lista.Count,
                LicenciasAsignadas = lista.Count(l => !l.Libre),
                LicenciasLibres = lista.Count(l => l.Libre),
                LicenciasVencidas = lista.Count(l => l.Vencida),
                Licencias = lista
            };
        }

        public async Task<(bool Exito, string Mensaje)> CrearLicenciaIndividualAsync(
            CrearLicenciaIndividualModel model)
        {
            try
            {
                var inv = await _context.Instituciones_Inventario_Licencias
                    .FirstOrDefaultAsync(i => i.Id_Institucion == model.Id_Institucion);
                if (inv == null)
                    return (false, "No se encontró el inventario de la institución.");

                _context.Licencias_Inventario_Individual.Add(new LicenciasInventarioIndividualDB
                {
                    Id_Institucion = model.Id_Institucion,
                    Id_Licencia = inv.Id_Licencia,
                    Fecha_Compra = DateTime.Now,
                    Fecha_Vencimiento = DateTime.Now.AddMonths(model.MesesVigencia),
                    Id_Usuario = model.Id_Usuario,
                    Activo = true
                });

                inv.Cantidad_Total++;
                if (model.Id_Usuario.HasValue)
                    inv.Cantidad_Asignada++;

                await _context.SaveChangesAsync();
                return (true, "Licencia creada correctamente.");
            }
            catch (Exception ex)
            {
                return (false, $"Error interno: {ex.Message}");
            }
        }

        public async Task<(bool Exito, string Mensaje)> EliminarLicenciaIndividualAsync(int idLicencia)
        {
            try
            {
                var lic = await _context.Licencias_Inventario_Individual.FindAsync(idLicencia);
                if (lic == null) return (false, "Licencia no encontrada.");

                if (lic.Id_Usuario.HasValue)
                    return (false, "No puedes eliminar una licencia que está asignada a un usuario. Desasígnala primero.");

                var inv = await _context.Instituciones_Inventario_Licencias
                    .FirstOrDefaultAsync(i => i.Id_Institucion == lic.Id_Institucion);
                if (inv != null)
                    inv.Cantidad_Total = Math.Max(0, inv.Cantidad_Total - 1);

                _context.Licencias_Inventario_Individual.Remove(lic);
                await _context.SaveChangesAsync();
                return (true, "Licencia eliminada correctamente.");
            }
            catch (Exception ex)
            {
                return (false, $"Error interno: {ex.Message}");
            }
        }

        public async Task<(bool Exito, string Mensaje)> RenovarLicenciaIndividualAsync(
            RenovarLicenciaModel model)
        {
            try
            {
                var lic = await _context.Licencias_Inventario_Individual.FindAsync(model.Id_Licencia_Individual);
                if (lic == null) return (false, "Licencia no encontrada.");

                // Si está vencida, renovar desde hoy; si no, extender desde su vencimiento actual
                DateTime base_ = lic.Vencida ? DateTime.Now : lic.Fecha_Vencimiento;
                lic.Fecha_Vencimiento = base_.AddMonths(model.MesesRenovacion);

                await _context.SaveChangesAsync();
                return (true, $"Licencia renovada hasta {lic.Fecha_Vencimiento:dd/MMM/yyyy}.");
            }
            catch (Exception ex)
            {
                return (false, $"Error interno: {ex.Message}");
            }
        }

        public async Task<(bool Exito, string Mensaje)> DesasignarLicenciaManualAsync(int idLicencia)
        {
            try
            {
                var lic = await _context.Licencias_Inventario_Individual.FindAsync(idLicencia);
                if (lic == null) return (false, "Licencia no encontrada.");
                if (!lic.Id_Usuario.HasValue) return (false, "Esta licencia ya está libre.");

                lic.Id_Usuario = null;

                var inv = await _context.Instituciones_Inventario_Licencias
                    .FirstOrDefaultAsync(i => i.Id_Institucion == lic.Id_Institucion);
                if (inv != null)
                    inv.Cantidad_Asignada = Math.Max(0, inv.Cantidad_Asignada - 1);

                await _context.SaveChangesAsync();
                return (true, "Licencia desasignada correctamente.");
            }
            catch (Exception ex)
            {
                return (false, $"Error interno: {ex.Message}");
            }
        }

    }
}
