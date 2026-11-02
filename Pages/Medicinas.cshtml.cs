using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;

namespace CarnetSaludWeb.Pages
{
    public class MedicinasModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public MedicinasModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public List<MedicamentoViewModel> ListaMedicamentos { get; set; } = new();

        public async Task OnGetAsync()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection")!;
            string userEmail = User.Identity?.Name ?? "";

            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string query = @"
                    SELECT m.Nombre, m.Dosis, m.Frecuencia, m.Indicaciones, m.FechaPrescripcion
                    FROM medicamentos m
                    INNER JOIN usuarios u ON m.UsuarioId = u.Id
                    WHERE u.Email = @Email
                    ORDER BY m.FechaPrescripcion DESC";

                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@Email", userEmail);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            ListaMedicamentos.Add(new MedicamentoViewModel
                            {
                                Nombre = reader["Nombre"].ToString()!,
                                Dosis = reader["Dosis"].ToString()!,
                                Frecuencia = reader["Frecuencia"].ToString()!,
                                Indicaciones = reader["Indicaciones"]?.ToString() ?? "",
                                FechaPrescripcion = Convert.ToDateTime(reader["FechaPrescripcion"]).ToString("dd/MM/yyyy")
                            });
                        }
                    }
                }
            }
        }
    }

    public class MedicamentoViewModel
    {
        public string Nombre { get; set; } = string.Empty;
        public string Dosis { get; set; } = string.Empty;
        public string Frecuencia { get; set; } = string.Empty;
        public string Indicaciones { get; set; } = string.Empty;
        public string FechaPrescripcion { get; set; } = string.Empty;
    }
}