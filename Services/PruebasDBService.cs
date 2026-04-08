using AnzanMegaArithmetics.Models;
using DataBase;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AnzanMegaArithmetics.Services
{
    public class PruebasDBService : IPruebasDBService
    {
        private readonly AnzanMegaContext _context;
        private readonly IUsersDBService _usersDBService;
        public PruebasDBService(AnzanMegaContext context, IUsersDBService usersDBService)
        {
            this._context = context;
            this._usersDBService = usersDBService;
        }

        public ConfiguracionPruebaModel ObtenerConfiguracionPorId(int idPrueba)
        {
            try
            {
                return _context.Pruebas
                    .Where(p => p.Id_Prueba == idPrueba)
                    .Select(p => new ConfiguracionPruebaModel
                    {
                        Id_Prueba = p.Id_Prueba,
                        TipoPrueba = p.Tipo_Prueba,
                        DatosConfiguracion = p.Configuracion
                    })
                    .FirstOrDefault();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public List<string> ObtenerTiposDePruebaDisponibles()
        {
            try
            {
                return _context.Pruebas
                    .Select(p => p.Tipo_Prueba)
                    .Distinct()
                    .OrderBy(t => t)
                    .ToList();
            }
            catch (Exception)
            {
                return new List<string>();
            }
        }

        public List<HistorialPruebaModel> ObtenerHistorialFiltrado(string clase, string alumno, string tipoPrueba, DateTime? fechaInicio, DateTime? fechaFin)
        {
            try
            {
                // 1. Iniciamos la consulta base
                var query = _context.Pruebas
                    .Include(p => p.Usuario)
                    .ThenInclude(u => u.Usuario_Clase)
                    .ThenInclude(uc => uc.Clase)
                    .Where(p => p.Activo == true)
                    .AsQueryable();

                // 2. Filtramos dinámicamente si los parámetros no vienen vacíos
                if (!string.IsNullOrEmpty(clase))
                {
                    query = query.Where(p => p.Usuario.Usuario_Clase.Any(uc => uc.Clase.Nombre == clase));
                }

                if (!string.IsNullOrEmpty(alumno))
                {
                    string search = alumno.ToLower().Trim();
                    query = query.Where(p => p.Usuario.Nombre.ToLower().Contains(search) ||
                                             p.Usuario.Gamer_Tag.ToLower().Contains(search));
                }

                if (!string.IsNullOrEmpty(tipoPrueba))
                {
                    query = query.Where(p => p.Tipo_Prueba == tipoPrueba);
                }

                if (fechaInicio.HasValue)
                {
                    query = query.Where(p => p.Fecha.Date >= fechaInicio.Value.Date);
                }

                if (fechaFin.HasValue)
                {
                    query = query.Where(p => p.Fecha.Date <= fechaFin.Value.Date);
                }

                // 3. Ordenamos, limitamos a 100 para no matar el navegador y mapeamos
                return query.OrderByDescending(p => p.Fecha)
                    .Take(100)
                    .Select(p => new HistorialPruebaModel
                    {
                        Id_Prueba = p.Id_Prueba,
                        Fecha = p.Fecha,
                        NombreAlumno = p.Usuario.Nombre,
                        GamerTag = p.Usuario.Gamer_Tag,
                        TipoPrueba = p.Tipo_Prueba,
                        Aciertos = p.Respuestas_Correctas,
                        TotalPreguntas = p.Total_Preguntas,
                        Tiempo = p.Tiempo,
                        XP = p.ExperienciaAdquirida,
                        // Calculamos el porcentaje de efectividad protegiendo contra división por cero
                        Efectividad = p.Total_Preguntas > 0
                                      ? (int)Math.Round((double)p.Respuestas_Correctas / p.Total_Preguntas * 100)
                                      : 0
                    })
                    .ToList();
            }
            catch (Exception)
            {
                return new List<HistorialPruebaModel>();
            }
        }


        //panel profes
        public ResumenClaseViewModel ObtenerResumenPorClase(string claseSeleccionada)
        {
            var viewModel = new ResumenClaseViewModel
            {
                ClaseActual = claseSeleccionada
            };

            DateTime fechaCorte = DateTime.Now.AddDays(-7);

            try
            {
                var datosPruebas = _context.Pruebas
                    .Where(p => p.Ids_Clases.Contains(claseSeleccionada) && p.Activo)
                    .Select(p => new
                    {
                        p.Id_Usuario,
                        p.Tipo_Prueba,
                        p.Total_Preguntas,
                        p.Respuestas_Correctas,
                        p.Fecha
                    })
                    .ToList();

                if (!datosPruebas.Any())
                    return viewModel;

                viewModel.TotalPruebasRealizadas = datosPruebas.Count;
                viewModel.TotalAlumnosActivos = datosPruebas
                    .Where(p => p.Fecha >= fechaCorte) // Solo los que practicaron en la última semana
                    .Select(p => p.Id_Usuario)
                    .Distinct()
                    .Count();

                var totalPreguntasGral = datosPruebas.Sum(p => p.Total_Preguntas);
                var totalAciertosGral = datosPruebas.Sum(p => p.Respuestas_Correctas);

                viewModel.PromedioGeneralClase = totalPreguntasGral > 0
                    ? Math.Round((double)totalAciertosGral / totalPreguntasGral * 100, 2)
                    : 0;

                var agrupadoPorTipo = datosPruebas.GroupBy(p => p.Tipo_Prueba).ToList();

                foreach (var grupo in agrupadoPorTipo)
                {
                    var tipo = grupo.Key ?? "Sin Categoría";

                    viewModel.DistribucionPruebas.Add(tipo, grupo.Count());

                    var preguntasDelTipo = grupo.Sum(g => g.Total_Preguntas);
                    var aciertosDelTipo = grupo.Sum(g => g.Respuestas_Correctas);

                    var promedioTipo = preguntasDelTipo > 0
                        ? Math.Round((double)aciertosDelTipo / preguntasDelTipo * 100, 2)
                        : 0;

                    viewModel.RendimientoPorActividad.Add(tipo, promedioTipo);
                }

                return viewModel;
            }
            catch (Exception ex)
            {
                return viewModel;
            }
        }

        //panel profes
        public bool GuardarPrueba(PruebasDBModel model)
        {
            try
            {
                UsuariosDB usuario = _context.Usuarios.Include(u => u.Usuario_Clase).ThenInclude(uc=> uc.Clase).FirstOrDefault(u => u.Id_Usuario == model.Id_Usuario);

                if (usuario == null)
                {
                    return false;
                }

                string clases = string.Join(", ", usuario.Usuario_Clase.Where(uc => uc.Clase != null).Select(uc => uc.Clase.Nombre));

                string configuracionLimpia = string.Empty;
                if (!string.IsNullOrEmpty(model.Configuracion))
                {
                    string configJson = model.Configuracion;


                    configJson = configJson.Replace("\\r", "").Replace("\\n", "").Replace("\r", "").Replace("\n", "");


                    if (model.Tipo_Prueba == "Suma Resta" || model.Tipo_Prueba == "Números Flash" || model.Tipo_Prueba == "Dictado Flash" || model.Tipo_Prueba.Contains("velocidad"))
                    {
                        try
                        {

                            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(configJson);

                            if (dict != null && dict.TryGetValue("TipoOperacion", out JsonElement tipoOpElement))
                            {
                                string tipoOp = tipoOpElement.GetString();

                                if (tipoOp == "suma")
                                {
                                    dict.Remove("DigitosResta");
                                    dict.Remove("DirectaResta");
                                }
                                else if (tipoOp == "resta")
                                {
                                    dict.Remove("DigitosSuma");
                                    dict.Remove("DirectaSuma");
                                }
                            }

                            // Volvemos a armar el JSON ya filtrado
                            configJson = JsonSerializer.Serialize(dict);
                        }
                        catch
                        {
                        }
                    }

                    configJson = Regex.Unescape(configJson);

                    configuracionLimpia = configJson
                        .Replace("{", "")
                        .Replace("}", "")
                        .Replace("\"", "")
                        .Replace("\r", "")
                        .Replace("\n", "")
                        .Replace(",", ", ")
                        .Replace(":", ": ")
                        .Replace("  ", " ");
                }

                PruebasDB nuevaPrueba = new PruebasDB
                {
                    Id_Usuario = usuario.Id_Usuario,
                    Ids_Clases = clases,
                    Activo = usuario.Activo,
                    Tiempo = model.Tiempo,
                    Total_Preguntas = model.Total_Preguntas,
                    Respuestas_Correctas = model.Respuestas_Correctas,
                    Fecha = model.Fecha,
                    ExperienciaAdquirida = model.ExperienciaAdquirida,
                    Tipo_Prueba = model.Tipo_Prueba,
                    Configuracion = configuracionLimpia
                };

                //calcular racha
                CalcularRacha(usuario, model.Fecha);

                usuario.Experiencia_Total += model.ExperienciaAdquirida;

                ActualizarRangoUsuario(usuario);

                _context.Usuarios.Update(usuario);
                _context.Pruebas.Add(nuevaPrueba);
                _context.SaveChanges();
                return true;

            }
            catch (Exception ex)
            {
                return false;
            }
        }


        public void CalcularRacha(UsuariosDB usuario, DateTime fechaActual)
        {
            DateTime hoy = fechaActual.Date;
            DateTime ayer = hoy.AddDays(-1);

            // Verificar si ya hizo prueba hoy
            bool yaHizoPruebaHoy = _context.Pruebas
                .Any(p => p.Id_Usuario == usuario.Id_Usuario &&
                         p.Fecha.Date == hoy);

            if (yaHizoPruebaHoy)
            {
                return;
            }

            bool hizoPruebaAyer = _context.Pruebas
                .Any(p => p.Id_Usuario == usuario.Id_Usuario &&
                         p.Fecha.Date == ayer);

            if (hizoPruebaAyer)
            {
                usuario.Racha = usuario.Racha + 1;
            }
            else
            {
                usuario.Racha = 1;
            }
        }

        private void ActualizarRangoUsuario(UsuariosDB usuario)
        {

            string nuevoRango = _usersDBService.CalcularRango(usuario.Experiencia_Total);

            if (usuario.Rango_Actual != nuevoRango)
            {
                usuario.Rango_Actual = nuevoRango;

            }
        }

    }
}
