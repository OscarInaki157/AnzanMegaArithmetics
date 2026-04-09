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


    }
}
