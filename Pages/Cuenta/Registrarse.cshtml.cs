using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;

namespace CarnetSaludWeb.Pages.Cuenta
{
    public class RegistrarseModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public RegistrarseModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public string ErrorMensaje { get; set; } = string.Empty;

        public class InputModel
        {
            public string Nombre { get; set; } = string.Empty;
            public string Rut { get; set; } = string.Empty;
            public string Contacto { get; set; } = string.Empty;
            public string Emergencia { get; set; } = string.Empty;
            public string GrupoSanguineo { get; set; } = string.Empty;
            public string Prevision { get; set; } = string.Empty;
            public string TieneDiscapacidad { get; set; } = "No";
            public string TipoDiscapacidad { get; set; } = "Ninguna";
            public string Alergias { get; set; } = "Sin alergias";
            public string Correo { get; set; } = string.Empty;
            public string Clave { get; set; } = string.Empty;
            public int Edad { get; set; } = 18;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            string connectionString = _configuration.GetConnectionString("DefaultConnection")!;

            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // 1. Validar si ya existe el correo o RUT
                    string checkQuery = "SELECT COUNT(*) FROM usuarios WHERE Email = @correo OR Rut = @rut";
                    using (var checkCmd = new MySqlCommand(checkQuery, connection))
                    {
                        checkCmd.Parameters.AddWithValue("@correo", Input.Correo);
                        checkCmd.Parameters.AddWithValue("@rut", Input.Rut);

                        long count = Convert.ToInt64(await checkCmd.ExecuteScalarAsync() ?? 0);
                        if (count > 0)
                        {
                            ErrorMensaje = "El RUT o correo ingresado ya se encuentra registrado.";
                            return Page();
                        }
                    }

                    // 2. Insertar en usuarios
                    string query = "INSERT INTO usuarios (Email, PasswordHash, Rut, Rol) VALUES (@correo, @clave, @rut, 'Paciente'); SELECT LAST_INSERT_ID();";

                    long nuevoUsuarioId = 0;

                    using (var cmdUsuario = new MySqlCommand(query, connection))
                    {
                        cmdUsuario.Parameters.AddWithValue("@correo", Input.Correo);
                        cmdUsuario.Parameters.AddWithValue("@clave", Input.Clave);
                        cmdUsuario.Parameters.AddWithValue("@rut", Input.Rut);

                        nuevoUsuarioId = Convert.ToInt64(await cmdUsuario.ExecuteScalarAsync());
                    }

                    // 3. Insertar en pacientes
                    string insertPacienteQuery = @"INSERT INTO pacientes 
                        (UsuarioId, Rut, NombreCompleto, Edad, Prevision, TieneDiscapacidad, TipoDiscapacidad, Alergias)
                        VALUES 
                        (@usuarioId, @rut, @nombre, @edad, @prevision, @tieneDiscapacidad, @tipoDiscapacidad, @alergias)";

                    using (var cmdPaciente = new MySqlCommand(insertPacienteQuery, connection))
                    {
                        bool discapacidadBool = Input.TieneDiscapacidad == "Si" || Input.TieneDiscapacidad == "true" || Input.TieneDiscapacidad == "1";

                        cmdPaciente.Parameters.AddWithValue("@usuarioId", nuevoUsuarioId);
                        cmdPaciente.Parameters.AddWithValue("@rut", Input.Rut);
                        cmdPaciente.Parameters.AddWithValue("@nombre", Input.Nombre);
                        cmdPaciente.Parameters.AddWithValue("@edad", Input.Edad);
                        cmdPaciente.Parameters.AddWithValue("@prevision", Input.Prevision);
                        cmdPaciente.Parameters.AddWithValue("@tieneDiscapacidad", discapacidadBool ? 1 : 0);
                        cmdPaciente.Parameters.AddWithValue("@tipoDiscapacidad", string.IsNullOrWhiteSpace(Input.TipoDiscapacidad) ? "Ninguna" : Input.TipoDiscapacidad);
                        cmdPaciente.Parameters.AddWithValue("@alergias", string.IsNullOrWhiteSpace(Input.Alergias) ? "Sin alergias" : Input.Alergias);

                        await cmdPaciente.ExecuteNonQueryAsync();
                    }
                }

                return RedirectToPage("/Cuenta/IniciarSesion");
            }
            catch (Exception ex)
            {
                ErrorMensaje = $"Error al registrar: {ex.Message}";
                return Page();
            }
        }
    }
}