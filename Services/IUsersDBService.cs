using AnzanMegaArithmetics.Helpers;
using AnzanMegaArithmetics.Models;

namespace AnzanMegaArithmetics.Services
{
    public interface IUsersDBService
    {
        public LoginResponseModel ValidateLogin(string user, string pass);
        public LoginResponseModel ObtenerUserDashboard(int id_Usuario);
        public bool UltimaConexion(LoginResponseModel user);
        public List<RankingUsersModel> ObtenerRankingUsuarios(int cantidad = 0);
        public List<RankingSlideModel> ObtenerRankingsSlider(int cantidad = 10);
        public RankingSlideModel ObtenerRankingFiltrado(string periodo, string actividad, int cantidad = 10);
        public List<UsuarioBDModel> ObtenerAlumnosPorClase(string nombreClase);
        UsuarioBDModel ObtenerAlumnoPorId(int idUsuario);

      
        public string CalcularRango(int experienciaTotal);
        public string ActualizarDatosBasicosJugador(ActualizarUsuarioModel actualizar);

        public int ClaimChallengeReward(int userId, int retoId);
        public List<DailyChallengeViewModel> GetUserDailyChallenges(int userId);
        public List<DailyChallengeViewModel> GetMasterChallenges();
        public string GetDBStringForCountOrTime(TipoActividadEnum tipo);
        public bool ValidateChallengeCompletion(DailyChallengeViewModel reto, List<DataBase.PruebasDB> activities);

        HashSet<string> ObtenerModulosHabilitados(int idUsuario);
    }
}
