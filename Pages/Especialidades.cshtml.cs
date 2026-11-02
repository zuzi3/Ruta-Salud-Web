using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;

namespace CarnetSaludWeb.Pages
{
    [Authorize]
    public class EspecialidadesModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public EspecialidadesModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [BindProperty(SupportsGet = true)]
        public string EspecialidadSeleccionada { get; set; } = "Medicina General";

        public List<EspecialidadDto> Especialidades { get; set; } = new List<EspecialidadDto>();
        public List<ExamenResultadoDto> Examenes { get; set; } = new List<ExamenResultadoDto>();

        public async Task OnGetAsync(string? especialidad)
        {
            if (!string.IsNullOrWhiteSpace(especialidad))
            {
                EspecialidadSeleccionada = especialidad;
            }

            string connectionString = _configuration.GetConnectionString("DefaultConnection")!;

            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // 1. Cargar todas las especialidades leyendo únicamente Id y Nombre
                    string queryEspecialidades = "SELECT Id, Nombre FROM especialidades ORDER BY Id ASC";
                    using (var cmdEsp = new MySqlCommand(queryEspecialidades, connection))
                    {
                        using (var readerEsp = await cmdEsp.ExecuteReaderAsync())
                        {
                            Especialidades.Clear();
                            while (await readerEsp.ReadAsync())
                            {
                                string nombre = readerEsp["Nombre"]?.ToString() ?? "";
                                
                                Especialidades.Add(new EspecialidadDto
                                {
                                    Id = Convert.ToInt32(readerEsp["Id"]),
                                    Nombre = nombre,
                                    Icono = ObtenerIconoPorEspecialidad(nombre)
                                });
                            }
                        }
                    }

                    // 2. Cargar los exámenes pertenecientes a la especialidad seleccionada
                    string queryExamenes = @"
                        SELECT e.Id, e.Titulo, e.Fecha, e.Doctor, e.Categoria, e.Descripcion, e.ArchivoPdf
                        FROM examenes e
                        INNER JOIN especialidades esp ON e.EspecialidadId = esp.Id
                        WHERE esp.Nombre = @especialidad
                        ORDER BY e.Fecha DESC";

                    using (var cmdEx = new MySqlCommand(queryExamenes, connection))
                    {
                        cmdEx.Parameters.AddWithValue("@especialidad", EspecialidadSeleccionada);

                        using (var readerEx = await cmdEx.ExecuteReaderAsync())
                        {
                            Examenes.Clear();
                            while (await readerEx.ReadAsync())
                            {
                                Examenes.Add(new ExamenResultadoDto
                                {
                                    Id = Convert.ToInt32(readerEx["Id"]),
                                    Titulo = readerEx["Titulo"]?.ToString() ?? "",
                                    Fecha = readerEx["Fecha"] != DBNull.Value 
                                            ? Convert.ToDateTime(readerEx["Fecha"]).ToString("dd/MM/yyyy") 
                                            : "",
                                    Doctor = readerEx["Doctor"]?.ToString() ?? "",
                                    Categoria = readerEx["Categoria"]?.ToString() ?? "",
                                    Descripcion = readerEx["Descripcion"]?.ToString() ?? "",
                                    ArchivoPdf = readerEx["ArchivoPdf"]?.ToString() ?? ""
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en EspecialidadesModel: {ex.Message}");
            }
        }

        // Asigna íconos de Bootstrap automáticamente según el nombre de la especialidad
        private string ObtenerIconoPorEspecialidad(string nombre)
        {
            if (string.IsNullOrEmpty(nombre)) return "journal-check";

            string lower = nombre.ToLower();

            if (lower.Contains("medicina general")) return "person-badge";
            if (lower.Contains("nutrici")) return "heart-pulse";
            if (lower.Contains("odontolo")) return "emoji-smile";
            if (lower.Contains("oftalmo")) return "eye";
            if (lower.Contains("urolo")) return "activity";

            return "journal-check";
        }
    }

    public class EspecialidadDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Icono { get; set; } = string.Empty;
    }

}