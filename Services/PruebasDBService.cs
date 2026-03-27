using AnzanMegaArithmetics.Models;
using DataBase;
using Microsoft.EntityFrameworkCore;

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


        public List<PruebasDBModel> ObtenerPruebas()
        {
            try
            {
                List<PruebasDB> pruebasDB =_context.Pruebas.ToList();

                if (pruebasDB == null || pruebasDB.Count == 0)
                {
                    return new List<PruebasDBModel>();
                }

                List<PruebasDBModel> pruebasModel = new();
                foreach (var prueba in pruebasDB)
                {

                    var usuario = _context.Usuarios.FirstOrDefault(u => u.Id_Usuario == prueba.Id_Usuario);

                    PruebasDBModel modelo = new PruebasDBModel
                    {
                        Id_Prueba = prueba.Id_Prueba,
                        Id_Usuario = prueba.Id_Usuario,
                        Ids_Clases = prueba.Ids_Clases,
                        Activo = prueba.Activo,
                        Tiempo = prueba.Tiempo,
                        Total_Preguntas = prueba.Total_Preguntas,
                        Respuestas_Correctas = prueba.Respuestas_Correctas,
                        Fecha = prueba.Fecha,
                        ExperienciaAdquirida = prueba.ExperienciaAdquirida,
                        Tipo_Prueba = prueba.Tipo_Prueba,

                        NombreUsuario = usuario != null ? usuario.Nombre : "No obtenido",
                        GamertagUsuario = usuario != null ? usuario.Gamer_Tag : "No obtenido"
                    };
                    pruebasModel.Add(modelo);
                }
                return pruebasModel;
            }
            catch (Exception ex)
            {
                return new List<PruebasDBModel>();
            }
        }

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
                    configuracionLimpia = model.Configuracion
                        .Replace("{", "")
                        .Replace("}", "")
                        .Replace("\"", "")
                        .Replace(",", ", ")
                        .Replace(":", ": ");
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
