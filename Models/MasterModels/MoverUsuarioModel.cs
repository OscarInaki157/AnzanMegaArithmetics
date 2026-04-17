namespace AnzanMegaArithmetics.Models.MasterModels
{
    public class MoverUsuarioModel
    {
        public int Id_Usuario { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string RolUsuario { get; set; } = string.Empty;
        public int Id_Institucion_Origen { get; set; }
        public string NombreInstitucionOrigen { get; set; } = string.Empty;

        // Destino seleccionado
        public int Id_Institucion_Destino { get; set; }
        public int Id_Clase_Destino { get; set; }

        // Para poblar dropdowns
        public List<InstitucionOpcionModel> InstitucionesDisponibles { get; set; } = new();
        public List<ClaseSedeModel> ClasesDestino { get; set; } = new();
    }
}
