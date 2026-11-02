using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;
using System.Security.Claims;

namespace CarnetSaludWeb.Pages.Citas
{
    [Authorize]
    public class AgendarModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public AgendarModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [BindProperty]
        public CitaInput Input { get; set; } = new();
        // AGREGAR AQUÍ LA PROPIEDAD QUE FALTABA:
        public string? MensajeExito { get; set; }
        public string? MensajeError { get; set; }

        public List<string> Especialidades { get; set; } = new() 
        { 
            "Medicina General", "Pediatría", "Odontología", "Kinesiología", "Psicología" 
        };

        public List<string> HorasDisponibles { get; set; } = new() 
        { 
            "08:30", "09:15", "10:00", "11:30", "14:00", "15:30", "16:15" 
        };

        public List<string> CentrosSalud { get; set; } = new() 
        { 
            "CESFAM San Rafael", "Centro Medico Central", "Hospital Parroquial" 
        };

        public IActionResult OnGet(string? rut)
        {
            // BLOQUEO 1: Si se pasa un RUT por parámetro (Flujo de consulta médica/PIN)
            if (!string.IsNullOrWhiteSpace(rut))
            {
                TempData["MensajeError"] = "Acceso Denegado: Los profesionales de la salud en modo consulta no pueden agendar citas para el paciente.";
                return RedirectToPage("/Carnet", new { rut = rut });
            }

            // BLOQUEO 2: Verificar que el usuario tenga una sesión activa real
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Cuenta/IniciarSesion");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string? rut)
        {
            // BLOQUEO 3: Cancelar procesamientos POST solicitados en modo consulta médica
            if (!string.IsNullOrWhiteSpace(rut))
            {
                TempData["MensajeError"] = "Acceso Denegado: No tiene permisos para modificar la agenda del paciente.";
                return RedirectToPage("/Carnet", new { rut = rut });
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Obtener identificador único del usuario autenticado
            string identificadorUsuario = User.FindFirst(ClaimTypes.Email)?.Value 
                                        ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                                        ?? User.FindFirst(ClaimTypes.Name)?.Value 
                                        ?? User.Identity?.Name 
                                        ?? "";

            if (string.IsNullOrEmpty(identificadorUsuario))
            {
                ModelState.AddModelError("", "Sesión inválida. Por favor, vuelva a iniciar sesión.");
                return Page();
            }

            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection")!;

                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    int usuarioId = 0;

                    // Buscar el ID exacto del usuario autenticado
                    string queryUsuario = "SELECT Id FROM usuarios WHERE LOWER(Email) = LOWER(@identificador) OR Rut = @identificador LIMIT 1";
                    using (var cmdUser = new MySqlCommand(queryUsuario, connection))
                    {
                        cmdUser.Parameters.AddWithValue("@identificador", identificadorUsuario.Trim());
                        var result = await cmdUser.ExecuteScalarAsync();
                        if (result != null && result != DBNull.Value)
                        {
                            usuarioId = Convert.ToInt32(result);
                        }
                    }

                    // Si no coincide con un usuario de la BD, se rechaza la operación
                    if (usuarioId == 0)
                    {
                        ModelState.AddModelError("", "No se pudo vincular la cita a un perfil de paciente activo.");
                        return Page();
                    }

                    // Insertar la cita asignada al usuario validado
                    string insertQuery = @"
                        INSERT INTO citas (UsuarioId, Doctor, Especialidad, Centro, Fecha, Hora, Estado) 
                        VALUES (@UsuarioId, @Doctor, @Especialidad, @Centro, @Fecha, @Hora, 'Confirmada')";

                    using (var cmd = new MySqlCommand(insertQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@UsuarioId", usuarioId);
                        cmd.Parameters.AddWithValue("@Doctor", Input.Doctor);
                        cmd.Parameters.AddWithValue("@Especialidad", Input.Especialidad);
                        cmd.Parameters.AddWithValue("@Centro", Input.CentroSalud);
                        cmd.Parameters.AddWithValue("@Fecha", Input.Fecha);
                        cmd.Parameters.AddWithValue("@Hora", Input.Hora);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return RedirectToPage("/Carnet");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error al guardar la cita: {ex.Message}");
                return Page();
            }
        }

        public class CitaInput
        {
            public string Especialidad { get; set; } = string.Empty;
            public DateTime Fecha { get; set; } = DateTime.Now;
            public string Hora { get; set; } = string.Empty;
            public string Doctor { get; set; } = string.Empty;
            public string CentroSalud { get; set; } = string.Empty;
        }
    }
}