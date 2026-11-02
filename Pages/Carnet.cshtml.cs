using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;

namespace CarnetSaludWeb.Pages
{
    [AllowAnonymous]
    public class CarnetModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public CarnetModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public PacienteViewModel Paciente { get; set; } = new PacienteViewModel();
        public List<CitaViewModel> ListaCitas { get; set; } = new List<CitaViewModel>();
        public bool EsConsultaMedica { get; set; } = false;

        public async Task<IActionResult> OnGetAsync(string? rut)
        {
            string identificadorUsuario = "";

            // 1. Si viene un RUT explícito por parámetro (Consulta Médica autorizada por PIN)
            if (!string.IsNullOrWhiteSpace(rut))
            {
                identificadorUsuario = rut.Trim();
                EsConsultaMedica = true;
            }
            // 2. Si no viene RUT, se consulta la sesión activa del paciente autenticado
            else if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                identificadorUsuario = User.FindFirst(ClaimTypes.Email)?.Value 
                                    ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                                    ?? User.Identity.Name 
                                    ?? "";
            }
            else
            {
                return RedirectToPage("/Cuenta/IniciarSesion");
            }

            if (string.IsNullOrEmpty(identificadorUsuario))
            {
                return RedirectToPage("/Cuenta/IniciarSesion");
            }

            await CargarDatosAsync(identificadorUsuario);

            return Page();
        }

        // ACCIÓN PROTEGIDA: Anular cita solo disponible para el paciente
        public async Task<IActionResult> OnPostAnularCitaAsync(int citaId, string? rut)
        {
            // BLOQUEO MÉDICO: Si la solicitud incluye un RUT por parámetro, se rechaza la cancelación
            if (!string.IsNullOrWhiteSpace(rut))
            {
                TempData["MensajeError"] = "Acceso Denegado: Los profesionales de la salud en modo lectura no pueden cancelar citas del paciente.";
                return RedirectToPage("/Carnet", new { rut = rut });
            }

            // Verificar que el paciente esté autenticado para procesar la cancelación
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Cuenta/IniciarSesion");
            }

            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection")!;
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    
                    string updateQuery = "UPDATE citas SET Estado = 'Anulada' WHERE Id = @Id";
                    using (var cmd = new MySqlCommand(updateQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@Id", citaId);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error al anular la cita: " + ex.Message);
            }

            return RedirectToPage("/Carnet");
        }

        private async Task CargarDatosAsync(string identificador)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection")!;

            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // 1. Cargar Datos Principales del Paciente
                    string queryPaciente = @"
                        SELECT p.Id, p.UsuarioId, p.NombreCompleto, p.Rut, p.Edad, p.Prevision, 
                               p.TieneDiscapacidad, p.TipoDiscapacidad, p.GrupoSanguineo, p.Alergias, 
                               p.ContactoEmergencia, u.Email
                        FROM usuarios u
                        LEFT JOIN pacientes p ON p.UsuarioId = u.Id OR p.Rut = @Identificador
                        WHERE LOWER(u.Email) = LOWER(@Identificador)
                           OR p.Rut = @Identificador
                           OR CAST(u.Id AS CHAR) = @Identificador
                        LIMIT 1";

                    using (var cmdUser = new MySqlCommand(queryPaciente, connection))
                    {
                        cmdUser.Parameters.AddWithValue("@Identificador", identificador.Trim());

                        using (var readerUser = await cmdUser.ExecuteReaderAsync())
                        {
                            if (await readerUser.ReadAsync())
                            {
                                Paciente = new PacienteViewModel
                                {
                                    Id = readerUser["UsuarioId"] != DBNull.Value ? Convert.ToInt32(readerUser["UsuarioId"]) : 0,
                                    Nombre = readerUser["NombreCompleto"] != DBNull.Value ? readerUser["NombreCompleto"].ToString()! : identificador,
                                    NombreCompleto = readerUser["NombreCompleto"] != DBNull.Value ? readerUser["NombreCompleto"].ToString()! : identificador,
                                    Rut = readerUser["Rut"]?.ToString() ?? "Sin RUT",
                                    Edad = readerUser["Edad"] != DBNull.Value ? Convert.ToInt32(readerUser["Edad"]) : 0,
                                    Prevision = readerUser["Prevision"]?.ToString() ?? "Fonasa",
                                    TipoSangre = readerUser["GrupoSanguineo"]?.ToString() ?? "O (+)",
                                    Alergias = readerUser["Alergias"] != DBNull.Value && !string.IsNullOrWhiteSpace(readerUser["Alergias"].ToString()) 
                                               ? readerUser["Alergias"].ToString()! 
                                               : "Sin alergias registradas",
                                    ContactoEmergencia = readerUser["ContactoEmergencia"]?.ToString() ?? "",
                                    TieneDiscapacidad = readerUser["TieneDiscapacidad"] != DBNull.Value && Convert.ToBoolean(readerUser["TieneDiscapacidad"]),
                                    TipoDiscapacidad = readerUser["TipoDiscapacidad"]?.ToString() ?? "Ninguna",
                                    SaludMentalInfo = "Sin observaciones registradas"
                                };
                            }
                            else
                            {
                                Paciente.NombreCompleto = identificador;
                                Paciente.Nombre = identificador.Contains("@") ? identificador.Split('@')[0] : identificador;
                            }
                        }
                    }

                    // 2. Cargar Citas Médicas Activas
                    string queryCitas = @"
                        SELECT c.Id, c.Doctor, c.Especialidad, c.Centro, c.Fecha, c.Hora, c.Estado 
                        FROM citas c
                        INNER JOIN usuarios u ON c.UsuarioId = u.Id
                        LEFT JOIN pacientes p ON p.UsuarioId = u.Id OR p.Rut = @Identificador
                        WHERE (LOWER(u.Email) = LOWER(@Identificador) OR p.Rut = @Identificador OR CAST(u.Id AS CHAR) = @Identificador)
                          AND c.Estado = 'Confirmada'
                          AND c.Fecha >= CURRENT_DATE()
                        ORDER BY c.Fecha ASC, c.Hora ASC";

                    using (var cmdCitas = new MySqlCommand(queryCitas, connection))
                    {
                        cmdCitas.Parameters.AddWithValue("@Identificador", identificador.Trim());

                        using (var readerCitas = await cmdCitas.ExecuteReaderAsync())
                        {
                            while (await readerCitas.ReadAsync())
                            {
                                ListaCitas.Add(new CitaViewModel
                                {
                                    Id = Convert.ToInt32(readerCitas["Id"]),
                                    Doctor = readerCitas["Doctor"]?.ToString() ?? "Por asignar",
                                    Especialidad = readerCitas["Especialidad"]?.ToString() ?? "",
                                    Fecha = readerCitas["Fecha"] != DBNull.Value 
                                            ? Convert.ToDateTime(readerCitas["Fecha"]).ToString("dd/MM/yyyy") 
                                            : "",
                                    Hora = readerCitas["Hora"]?.ToString() ?? "",
                                    Estado = readerCitas["Estado"]?.ToString() ?? "Confirmada"
                                });
                            }
                        }
                    }

                    // 3. Cargar Medicamentos del Paciente
                    string queryMedicamentos = @"
                        SELECT m.Id, m.Nombre, m.Dosis, m.Frecuencia, m.Indicaciones
                        FROM medicamentos m
                        INNER JOIN usuarios u ON m.UsuarioId = u.Id
                        LEFT JOIN pacientes p ON p.UsuarioId = u.Id OR p.Rut = @Identificador
                        WHERE LOWER(u.Email) = LOWER(@Identificador) OR p.Rut = @Identificador OR CAST(u.Id AS CHAR) = @Identificador";

                    using (var cmdMed = new MySqlCommand(queryMedicamentos, connection))
                    {
                        cmdMed.Parameters.AddWithValue("@Identificador", identificador.Trim());

                        using (var readerMed = await cmdMed.ExecuteReaderAsync())
                        {
                            while (await readerMed.ReadAsync())
                            {
                                Paciente.Medicamentos.Add(new MedicamentoViewModel
                                {
                                    Id = Convert.ToInt32(readerMed["Id"]),
                                    Nombre = readerMed["Nombre"]?.ToString() ?? "",
                                    Dosis = readerMed["Dosis"]?.ToString() ?? "",
                                    Frecuencia = readerMed["Frecuencia"]?.ToString() ?? "",
                                    Indicaciones = readerMed["Indicaciones"]?.ToString() ?? ""
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error CargarDatosAsync: " + ex.Message);
            }

            if (Paciente.Examenes == null) Paciente.Examenes = new List<ExamenViewModel>();
            if (Paciente.AntecedentesFamiliares == null) Paciente.AntecedentesFamiliares = new List<string>();
        }

        public class PacienteViewModel
        {
            public int Id { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public string NombreCompleto { get; set; } = string.Empty;
            public string Rut { get; set; } = string.Empty;
            public int Edad { get; set; }
            public string Prevision { get; set; } = string.Empty;
            public string Tipo { get; set; } = string.Empty;
            public string Subtitulo { get; set; } = string.Empty;
            public string Descripcion { get; set; } = string.Empty;
            public string TipoSangre { get; set; } = string.Empty;
            public string Alergias { get; set; } = string.Empty;
            public string ContactoEmergencia { get; set; } = string.Empty;
            public bool TieneDiscapacidad { get; set; } = false;
            public string TipoDiscapacidad { get; set; } = string.Empty;
            public string SaludMentalInfo { get; set; } = string.Empty;
            public List<MedicamentoViewModel> Medicamentos { get; set; } = new();
            public List<ExamenViewModel> Examenes { get; set; } = new();
            public List<string> AntecedentesFamiliares { get; set; } = new();
        }

        public class MedicamentoViewModel
        {
            public int Id { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public string Dosis { get; set; } = string.Empty;
            public string Frecuencia { get; set; } = string.Empty;
            public string Indicaciones { get; set; } = string.Empty;
        }

        public class ExamenViewModel
        {
            public int Id { get; set; }
            public string Titulo { get; set; } = string.Empty;
            public string Nombre { get; set; } = string.Empty;
            public string Tipo { get; set; } = string.Empty;
            public string Subtitulo { get; set; } = string.Empty;
            public string Descripcion { get; set; } = string.Empty;
            public string Fecha { get; set; } = string.Empty;
            public string FechaResultado { get; set; } = string.Empty;
            public string Doctor { get; set; } = string.Empty;
            public string Categoria { get; set; } = string.Empty;
            public string ArchivoPdf { get; set; } = string.Empty;
        }

        public class CitaViewModel
        {
            public int Id { get; set; }
            public string Doctor { get; set; } = string.Empty;
            public string Especialidad { get; set; } = string.Empty;
            public string Fecha { get; set; } = string.Empty;
            public string Hora { get; set; } = string.Empty;
            public string Estado { get; set; } = string.Empty;
        }
    }
}