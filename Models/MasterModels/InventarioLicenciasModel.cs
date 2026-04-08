namespace AnzanMegaArithmetics.Models.MasterModels
{
    public class InventarioLicenciasModel
    {
        public int Id_Inventario { get; set; }
        public int Id_Institucion { get; set; }
        public int Id_Licencia { get; set; }
        public int Cantidad_Total { get; set; }
        public int Cantidad_Asignada { get; set; }
    }
}
