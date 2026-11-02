using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;

namespace CarnetSaludWeb.Pages
{
    [AllowAnonymous]
    public class EmergenciaModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public EmergenciaModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public PacienteEmergenciaDto Paciente { get; set; } = new PacienteEmergenciaDto();
        public bool PacienteEncontrado { get; set; } = false;

        public async Task<IActionResult> OnGetAsync(string? rut)
        {
            if (string.IsNullOrEmpty(rut))
            {
                Paciente = new PacienteEmergenciaDto
                {
                    Nombre = "Lucy Gonzales",
                    Rut = "12.345.678-9",
                    GrupoSanguineo = "O (+)",
                    Alergias = "Penicilina",
                    Condiciones = "Hipertensión / Diabetes Tipo 2",
                    ContactoEmergencia = "+56912345678"
                };
                PacienteEncontrado = true;
                return Page();
            }

            string connectionString = _configuration.GetConnectionString("DefaultConnection")!;

            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                
                string query = @"
                    SELECT 
                        p.Rut,
                        p.NombreCompleto, 
                        p.Edad, 
                        p.Prevision, 
                        p.TieneDiscapacidad, 
                        p.TipoDiscapacidad,
                        p.GrupoSanguineo,
                        p.Alergias,
                        p.ContactoEmergencia,
                        u.Email
                    FROM pacientes p
                    LEFT JOIN usuarios u ON p.UsuarioId = u.Id
                    WHERE p.Rut = @rut";

                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@rut", rut);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            PacienteEncontrado = true;

                            Paciente.Rut = reader["Rut"] != DBNull.Value ? reader["Rut"].ToString()! : rut;
                            Paciente.Nombre = reader["NombreCompleto"] != DBNull.Value ? reader["NombreCompleto"].ToString()! : "";
                            Paciente.GrupoSanguineo = reader["GrupoSanguineo"] != DBNull.Value ? reader["GrupoSanguineo"].ToString()! : "O (+)";
                            Paciente.Alergias = reader["Alergias"] != DBNull.Value ? reader["Alergias"].ToString()! : "Sin alergias";
                            Paciente.ContactoEmergencia = reader["ContactoEmergencia"] != DBNull.Value ? reader["ContactoEmergencia"].ToString()! : "";

                            // --- LÓGICA SEPARADA PARA CONDICIONES CRÓNICAS ---
                            List<string> listaCondiciones = new List<string>();

                            // Evaluamos si el paciente registra alguna discapacidad o enfermedad crónica
                            if (reader["TieneDiscapacidad"] != DBNull.Value && Convert.ToBoolean(reader["TieneDiscapacidad"]))
                            {
                                string tipo = reader["TipoDiscapacidad"] != DBNull.Value ? reader["TipoDiscapacidad"].ToString()! : "";
                                if (!string.IsNullOrWhiteSpace(tipo) && tipo.Trim().ToLower() != "ninguna")
                                {
                                    listaCondiciones.Add($"Discapacidad: {tipo}");
                                }
                            }

                            // Si la lista está vacía, indicamos expresamente "Sin condiciones registradas"
                            if (listaCondiciones.Count > 0)
                            {
                                Paciente.Condiciones = string.Join(" | ", listaCondiciones);
                            }
                            else
                            {
                                Paciente.Condiciones = "Sin condiciones crónicas registradas";
                            }
                        }
                        else
                        {
                            PacienteEncontrado = false;
                        }
                    }
                }
            }

            return Page();
        }
    }

    public class PacienteEmergenciaDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Rut { get; set; } = string.Empty;
        public string GrupoSanguineo { get; set; } = string.Empty;
        public string Alergias { get; set; } = string.Empty;
        public string Condiciones { get; set; } = string.Empty;
        public string ContactoEmergencia { get; set; } = string.Empty;
    }
}