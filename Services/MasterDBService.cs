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
    }
}
