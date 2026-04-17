namespace AnzanMegaArithmetics.Models.MasterModels
{
    public class MoverClaseModel
    {
        public int Id_Clase { get; set; }
        public string NombreClase { get; set; } = string.Empty;
        public int CantidadUsuarios { get; set; }
        public int Id_Institucion_Origen { get; set; }
        public string NombreInstitucionOrigen { get; set; } = string.Empty;

        // Destino seleccionado
        public int Id_Institucion_Destino { get; set; }

        // Para poblar dropdown
        public List<InstitucionOpcionModel> InstitucionesDisponibles { get; set; } = new();
    }
}
