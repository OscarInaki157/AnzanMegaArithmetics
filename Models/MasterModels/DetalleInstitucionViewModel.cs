namespace AnzanMegaArithmetics.Models.MasterModels
{
    public class DetalleInstitucionViewModel
    {
        public int Id_Usuario { get; set; }
        public string Id_Rol { get; set; }
        public int Exp { get; set; }

        public int Id_Institucion { get; set; }
        public string Nombre { get; set; }
        public DateTime FechaRegistro { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public bool Activa { get; set; }

        public int TotalLicencias { get; set; }
        public int LicenciasEnUso { get; set; }
        public int TotalProfesores { get; set; }
        public int TotalAlumnos { get; set; }
        public int TotalClases { get; set; }
        public double PromedioGeneral { get; set; }

        public int DiasRestantes { get; set; }
        public int PorcentajeTiempoTranscurrido { get; set; }
        public List<string> LabelsActividad { get; set; } = new List<string>();
        public List<int> ValoresActividad { get; set; } = new List<int>();

        public List<ClaseSedeModel> ListaClases { get; set; } = new List<ClaseSedeModel>();

    }
}
