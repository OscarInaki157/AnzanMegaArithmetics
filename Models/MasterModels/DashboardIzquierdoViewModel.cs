namespace AnzanMegaArithmetics.Models.MasterModels
{
    public class DashboardIzquierdoViewModel
    {
        public int TotalEscuelas { get; set; }
        public int TotalAlumnos { get; set; }
        public int TotalLicenciasGeneradas { get; set; }
        public int LicenciasAsignadas { get; set; }
        public int LicenciasDisponibles { get; set; }

        public List<AlertaMasterModel> Alertas { get; set; } = new(); // ya existe
        public List<AlertaLicenciaProximaModel> LicenciasProximasAVencer { get; set; } = new();
        public List<AlertaUsuarioSinLicenciaModel> UsuariosSinLicencia { get; set; } = new();
        public List<AlertaClaseVaciaModel> ClasesVacias { get; set; } = new();
    }
}
