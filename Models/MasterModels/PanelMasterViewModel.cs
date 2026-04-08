namespace AnzanMegaArithmetics.Models.MasterModels
{
    public class PanelMasterViewModel
    {
        public int Id_Usuario { get; set; }
        public string Id_Rol { get; set; }
        public string Nombre { get; set; }
        public string Correo { get; set; }
        public string Gamer_Tag { get; set; }
        public int Exp { get; set; }
        public string Licencia { get; set; } = string.Empty;
        public DateTime? Ultima_Cnx { get; set; }
        public List<string> Clases { get; set; } = new();
        public DashboardIzquierdoViewModel DashboardIzquierdo { get; set; } = new DashboardIzquierdoViewModel();
        public List<InstitucionDirectorioModel> ListaInstituciones { get; set; } = new List<InstitucionDirectorioModel>();
    }
}
