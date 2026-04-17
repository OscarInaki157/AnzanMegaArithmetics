namespace AnzanMegaArithmetics.Models.MasterModels
{
    public class EliminarClaseModel
    {
        public int Id_Clase { get; set; }
        public string NombreClase { get; set; } = string.Empty;
        public int CantidadUsuarios { get; set; }
        public int Id_Clase_Destino { get; set; }
        public List<ClaseSedeModel> ClasesDisponibles { get; set; } = new();
    }
}
