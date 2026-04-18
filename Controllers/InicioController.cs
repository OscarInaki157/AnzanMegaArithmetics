using AnzanMegaArithmetics.Models;
using AnzanMegaArithmetics.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;

namespace AnzanMegaArithmetics.Controllers
{
    public class InicioController : Controller
    {
        private readonly IUsersDBService _usersDBService;
        public InicioController(IUsersDBService usersDBService) 
        {
            this._usersDBService = usersDBService;
        }
        
        public IActionResult Inicio()
        {
            return View();
        }

        public IActionResult Nosotros() 
        {
            return View();
        }

        public IActionResult Contacto()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Login() 
        {
            return View();
        }

        public IActionResult Registro() 
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ValidarUser(LoginRequestModel model)
        {
            LoginResponseModel response = _usersDBService.ValidateLogin(model.Usuario, model.Pass);

            // Si hay motivo de rechazo, regresar al login con el mensaje
            if (!string.IsNullOrEmpty(response.MotivoRechazo))
            {
                ViewBag.ErrorMessage = response.MotivoRechazo;
                return View("Login");
            }

            // Actualizar última conexión
            _usersDBService.UltimaConexion(response);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, response.Id_Usuario.ToString()),
                new Claim(ClaimTypes.Name, response.Nombre),
                new Claim(ClaimTypes.Role, response.Id_Rol),
                new Claim("Usuario", response.Gamer_Tag),
                new Claim("Correo", response.Correo),
                new Claim("Racha", response.Racha.ToString()),
                new Claim("Exp", response.Exp.ToString()),
                new Claim("UltimaCnx", response.Ultima_Cnx.ToString("yyyy-MM-dd HH:mm:ss")),
                new Claim("Licencia", response.Licencia),
                new Claim("Clases", string.Join(",", response.Clases))
            };

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                AllowRefresh = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30),
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            return RedirectToAction("Dashboard", "Dashboard");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Inicio", "Inicio");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnviarContacto(string Nombre, string Correo, string Telefono, string Asunto, string Mensaje)
        {
            try
            {
                string smtpHost = "mail.mentesmexico.com";
                int smtpPort = 587;
                string emailRemitente = "contacto@mentesmexico.com";
                string passwordRemitente = "MentesMe!";

                string correoDestino = "";

                switch (Asunto)
                {
                    case "Ventas":
                        correoDestino = "ventas@mentesmexico.com";
                        break;
                    case "Soporte Tecnico":
                        correoDestino = "soporte@mentesmexico.com";
                        break;
                    case "Alianzas Estrategicas":
                        correoDestino = "contacto@mentesmexico.com";
                        break;
                    default:
                        correoDestino = "contacto@mentesmexico.com";
                        break;
                }

                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(emailRemitente, "Web Mentes México");
                mail.To.Add(correoDestino);
                mail.Subject = $"Nuevo contacto web - Asunto: {Asunto}";
                mail.IsBodyHtml = true;

                mail.Body = $@"
                <h2 style='color: #010f36;'>Nuevo mensaje desde la web</h2>
                <hr />
                <p><strong>Nombre:</strong> {Nombre}</p>
                <p><strong>Correo del cliente:</strong> {Correo}</p>
                <p><strong>Teléfono:</strong> {Telefono}</p>
                <p><strong>Asunto:</strong> {Asunto}</p>
                <br />
                <p><strong>Mensaje:</strong></p>
                <p style='padding: 10px; background-color: #f1f5f9; border-left: 4px solid #4c4cff;'>
                    {Mensaje.Replace("\n", "<br/>")}
                </p>
            ";

                using (SmtpClient smtp = new SmtpClient(smtpHost, smtpPort))
                {
                    smtp.Credentials = new NetworkCredential(emailRemitente, passwordRemitente);
                    smtp.EnableSsl = true;

                    await smtp.SendMailAsync(mail);
                }

                ViewBag.SuccessMessage = "¡Tu mensaje ha sido enviado con éxito! Nos pondremos en contacto contigo muy pronto.";
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Hubo un error al enviar tu mensaje. Por favor, intenta de nuevo más tarde.";
            }

            return View("Contacto");
        }

    }
}
