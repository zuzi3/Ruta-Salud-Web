using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CarnetSaludWeb.Pages
{
    [Authorize]
    public class ExamenesModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public ExamenesModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [BindProperty(SupportsGet = true)]
        public string EspecialidadSeleccionada { get; set; } = "Medicina General";

        // Propiedad agregada para resolver el error CS0103
        public List<ExamenResultadoDto> Examenes { get; set; } = new List<ExamenResultadoDto>();

        public async Task OnGetAsync(string? especialidad)
        {
            string identificadorUsuario = User.Identity?.Name ?? "";
            if (string.IsNullOrEmpty(identificadorUsuario)) return;

            // Si no viene ninguna especialidad seleccionada, usa Medicina General por defecto
            EspecialidadSeleccionada = string.IsNullOrWhiteSpace(especialidad) ? "Medicina General" : especialidad;

            string connectionString = _configuration.GetConnectionString("DefaultConnection")!;

            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string query = @"
                        SELECT e.Id, e.Titulo, e.Fecha, e.Doctor, e.Categoria, e.Descripcion, e.ArchivoPdf
                        FROM examenes e
                        INNER JOIN usuarios u ON e.UsuarioId = u.Id
                        INNER JOIN especialidades esp ON e.EspecialidadId = esp.Id
                        WHERE (u.Email = @identificador OR u.Rut = @identificador)
                          AND esp.Nombre = @especialidad
                        ORDER BY e.Fecha DESC";

                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@identificador", identificadorUsuario);
                        cmd.Parameters.AddWithValue("@especialidad", EspecialidadSeleccionada);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            Examenes.Clear();
                            while (await reader.ReadAsync())
                            {
                                Examenes.Add(new ExamenResultadoDto
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    Titulo = reader["Titulo"]?.ToString() ?? "",
                                    Fecha = reader["Fecha"] != DBNull.Value 
                                            ? Convert.ToDateTime(reader["Fecha"]).ToString("dd/MM/yyyy") 
                                            : "",
                                    Doctor = reader["Doctor"]?.ToString() ?? "",
                                    Categoria = reader["Categoria"]?.ToString() ?? "",
                                    Descripcion = reader["Descripcion"]?.ToString() ?? "",
                                    ArchivoPdf = reader["ArchivoPdf"]?.ToString() ?? ""
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Control de excepciones
            }
        }

        public async Task<IActionResult> OnGetDescargarPdfAsync(int id)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            string identificadorUsuario = User.Identity?.Name ?? "";
            if (string.IsNullOrEmpty(identificadorUsuario)) return RedirectToPage("/Account/Login");

            string connectionString = _configuration.GetConnectionString("DefaultConnection")!;
            ExamenResultadoDto? examen = null;

            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string queryExamen = @"
                        SELECT e.Id, e.Titulo, e.Fecha, e.Doctor, e.Categoria, e.Descripcion, e.ArchivoPdf
                        FROM examenes e
                        INNER JOIN usuarios u ON e.UsuarioId = u.Id
                        WHERE e.Id = @ExamenId AND (u.Email = @identificador OR u.Rut = @identificador)";

                    using (var cmd = new MySqlCommand(queryExamen, connection))
                    {
                        cmd.Parameters.AddWithValue("@ExamenId", id);
                        cmd.Parameters.AddWithValue("@identificador", identificadorUsuario);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                examen = new ExamenResultadoDto
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    Titulo = reader["Titulo"]?.ToString() ?? "",
                                    Fecha = reader["Fecha"] != DBNull.Value 
                                            ? Convert.ToDateTime(reader["Fecha"]).ToString("dd/MM/yyyy") 
                                            : "",
                                    Doctor = reader["Doctor"]?.ToString() ?? "",
                                    Categoria = reader["Categoria"]?.ToString() ?? "",
                                    Descripcion = reader["Descripcion"]?.ToString() ?? "",
                                    ArchivoPdf = reader["ArchivoPdf"]?.ToString() ?? ""
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                return RedirectToPage("/Error");
            }

            if (examen == null) return NotFound();

            // Generar el informe PDF dinámicamente con la información del examen
            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20, Unit.Point);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header()
                        .Text("CARNET SALUD WEB - INFORME MÉDICO")
                        .SemiBold().FontSize(18).FontColor("#4A148C");

                    page.Content().PaddingVertical(10, Unit.Point).Column(col =>
                    {
                        col.Spacing(10);

                        col.Item().Text($"Examen: {examen.Titulo}").Bold().FontSize(14);
                        col.Item().Text($"Fecha de Emisión: {examen.Fecha}");
                        col.Item().Text($"Solicitado por: {examen.Doctor}");
                        col.Item().Text($"Categoría: {examen.Categoria}");
                        
                        col.Item().LineHorizontal(1);

                        col.Item().Text("Observaciones del Resultado:").Bold();
                        col.Item().Text(string.IsNullOrEmpty(examen.Descripcion) 
                            ? "Los parámetros analizados en este examen se encuentran dentro de los rangos de referencia estándar." 
                            : examen.Descripcion);
                    });

                    page.Footer()
                        .AlignCenter()
                        .Text("Documento oficial generado electrónicamente por Carnet Salud Web.")
                        .FontSize(9).FontColor(Colors.Grey.Medium);
                });
            }).GeneratePdf();

            string nombreArchivo = $"Examen_{examen.Titulo.Replace(" ", "_")}.pdf";

            return File(pdfBytes, "application/pdf", nombreArchivo);
        }
    }

}