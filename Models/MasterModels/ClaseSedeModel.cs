namespace AnzanMegaArithmetics.Models.MasterModels
{
    public class ClaseSedeModel
    {
        public int Id_Clase { get; set; }
        public string Nombre { get; set; }
        public string NombreInstitucion { get; set; } = string.Empty;
        public int Id_Institucion { get; set; }
        public int CantidadAlumnos { get; set; }
        public bool Activa { get; set; }
    }
}
