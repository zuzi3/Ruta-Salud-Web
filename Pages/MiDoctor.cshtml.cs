using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;

namespace CarnetSaludWeb.Pages
{
    [Authorize]
    public class MiDoctorModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public MiDoctorModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Propiedad que espera MiDoctor.cshtml para el doctor principal
        public DoctorViewModel DoctorCabecera { get; set; } = new DoctorViewModel();

        // Propiedad que espera MiDoctor.cshtml para la lista de historial
        public List<DoctorViewModel> HistorialDoctores { get; set; } = new List<DoctorViewModel>();

        public async Task OnGetAsync()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection")!;
            string userEmail = User.Identity?.Name ?? "";

            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Consulta adaptada a la tabla 'citas' real
                    string query = @"
    SELECT c.Doctor, c.Especialidad, c.Centro, c.Fecha, c.Hora
    FROM citas c
    INNER JOIN usuarios u ON c.UsuarioId = u.Id
    WHERE u.Email = @Email
    ORDER BY c.Fecha DESC, c.Hora DESC";

using (var cmd = new MySqlCommand(query, connection))
{
    cmd.Parameters.AddWithValue("@Email", userEmail);

    using (var reader = await cmd.ExecuteReaderAsync())
    {
        while (await reader.ReadAsync())
        {
            var doc = new DoctorViewModel
            {
                Nombre = reader["Doctor"]?.ToString() ?? "Doctor sin asignar",
                Especialidad = reader["Especialidad"]?.ToString() ?? "General",
                // Muestra el establecimiento seleccionado por el paciente al agendar
                CentroMedico = reader["Centro"]?.ToString() ?? "Red RutaSalud", 
                Telefono = "No registrado",
                Email = "No registrado"
            };

            HistorialDoctores.Add(doc);
        }
    }
}

                    if (HistorialDoctores.Any())
                    {
                        DoctorCabecera = HistorialDoctores.First();
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error al cargar doctor: {ex.Message}");
            }
        }
    }

    public class DoctorViewModel
    {
        public string Nombre { get; set; } = string.Empty;
        public string Especialidad { get; set; } = string.Empty;
        public string CentroMedico { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}