namespace AnzanMegaArithmetics.Models.MasterModels
{
    public class DashboardIzquierdoViewModel
    {
        public int TotalEscuelas { get; set; }
        public int TotalAlumnos { get; set; }
        public int TotalLicenciasGeneradas { get; set; }
        public int LicenciasAsignadas { get; set; }
        public int LicenciasDisponibles { get; set; }

        public List<AlertaMasterModel> Alertas { get; set; } = new List<AlertaMasterModel>();
    }
}
