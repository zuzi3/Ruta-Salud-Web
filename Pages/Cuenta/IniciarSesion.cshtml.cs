using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;

namespace CarnetSaludWeb.Pages.Cuenta
{
    public class IniciarSesionModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public IniciarSesionModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [BindProperty] public string Email { get; set; } = string.Empty;
        [BindProperty] public string Password { get; set; } = string.Empty;

        public string? ErrorMensaje { get; set; }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            string connectionString = _configuration.GetConnectionString("DefaultConnection")!;
            int usuarioId = 0;
            string rol = "";

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                // 1. Validar correo y contraseña en la base de datos
                string query = "SELECT Id, Rol FROM Usuarios WHERE Email = @Email AND PasswordHash = @Password";
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@Email", Email);
                    cmd.Parameters.AddWithValue("@Password", Password);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            usuarioId = reader.GetInt32("Id");
                            rol = reader.GetString("Rol");
                        }
                        else
                        {
                            ErrorMensaje = "Correo electrónico o contraseña incorrectos.";
                            return Page();
                        }
                    }
                }
            }

            // 2. Crear las credenciales (Claims) e iniciar la sesión con Cookie
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString()),
                new Claim(ClaimTypes.Email, Email),
                new Claim(ClaimTypes.Role, rol)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // 3. Redirigir a la vista del Carnet de Salud
            return RedirectToPage("/Carnet");
        }
    }
}