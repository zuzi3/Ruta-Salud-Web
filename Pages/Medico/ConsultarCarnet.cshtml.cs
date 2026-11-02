using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;
using System.Net;
using System.Net.Mail;

namespace CarnetSaludWeb.Pages.Medico
{
    public class ConsultarCarnetModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public ConsultarCarnetModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [BindProperty]
        public string RutPaciente { get; set; } = string.Empty;

        [BindProperty]
        public string CodigoPin { get; set; } = string.Empty;

        public bool EsperandoCodigo { get; set; } = false;
        public string EmailOculto { get; set; } = string.Empty;
        public string MensajeError { get; set; } = string.Empty;

        // Guarda temporalmente el PIN asignado al RUT
        private static readonly Dictionary<string, string> _pinesTemporales = new Dictionary<string, string>();

        public void OnGet() { }

        // MÉTODOS DE ACCIÓN
        public async Task<IActionResult> OnPostSolicitarAccesoAsync()
        {
            if (string.IsNullOrWhiteSpace(RutPaciente))
            {
                MensajeError = "Debe ingresar un RUT válido.";
                return Page();
            }

            string connectionString = _configuration.GetConnectionString("DefaultConnection")!;
            string emailPaciente = string.Empty;

            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                
                // Consulta el email de la tabla usuarios relacionándolo con el paciente
                string query = @"
                    SELECT u.Email 
                    FROM pacientes p 
                    INNER JOIN usuarios u ON p.UsuarioId = u.Id 
                    WHERE p.Rut = @rut";

                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@rut", RutPaciente);
                    var result = await cmd.ExecuteScalarAsync();
                    if (result != null)
                    {
                        emailPaciente = result.ToString()!;
                    }
                }
            }

            if (!string.IsNullOrEmpty(emailPaciente))
            {
                // Generar PIN aleatorio de 6 dígitos
                string pin = new Random().Next(100000, 999999).ToString();
                _pinesTemporales[RutPaciente] = pin;

                // Enviar el PIN al correo del paciente
                bool enviado = EnviarCorreoPIN(emailPaciente, pin);

                if (enviado)
                {
                    EsperandoCodigo = true;
                    var partes = emailPaciente.Split('@');
                    EmailOculto = partes[0].Length > 3 
                        ? partes[0].Substring(0, 3) + "***@" + partes[1] 
                        : "***@" + partes[1];
                    return Page();
                }
                else
                {
                    MensajeError = "Error al enviar el correo con el PIN. Intente nuevamente.";
                    return Page();
                }
            }

            MensajeError = "El RUT ingresado no está registrado o no posee una cuenta asociada.";
            return Page();
        }

        public IActionResult OnPostValidarCodigo()
        {
            if (_pinesTemporales.ContainsKey(RutPaciente) && _pinesTemporales[RutPaciente] == CodigoPin)
            {
                _pinesTemporales.Remove(RutPaciente); // Limpiar PIN tras uso
                
                // Redirige directamente a la ficha del paciente
                return RedirectToPage("/Emergencia", new { rut = RutPaciente });
            }

            MensajeError = "El código PIN es incorrecto o ha caducado.";
            EsperandoCodigo = true;
            return Page();
        }

        private bool EnviarCorreoPIN(string destino, string pin)
        {
            try
            {
                var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential("salassusanz914@gmail.com", "jsyv vzdb obpy ubyr"),
                    EnableSsl = true,
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress("salassusanz914@gmail.com", "RutaSalud Médica"),
                    Subject = "Código de Permiso - Ficha Médica RutaSalud",
                    Body = $"Un médico está solicitando acceso a tu Ficha Clínica en RutaSalud.\n\nTu PIN de autorización es: {pin}\n\nSi estás en consulta, dicte este código al profesional.",
                    IsBodyHtml = false,
                };
                mailMessage.To.Add(destino);

                smtpClient.Send(mailMessage);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}