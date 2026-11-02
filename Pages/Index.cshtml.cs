using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;

namespace CarnetSaludWeb.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public IndexModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [BindProperty] public string Email { get; set; } = string.Empty;
        [BindProperty] public string Password { get; set; } = string.Empty;

        public string MensajeError { get; set; } = string.Empty;

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            string connectionString = _configuration.GetConnectionString("DefaultConnection")!;

            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string query = "SELECT Id, Rut, Email FROM usuarios WHERE Email = @Email AND PasswordHash = @Password";
                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Email", Email);
                        cmd.Parameters.AddWithValue("@Password", Password);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                string userEmail = reader["Email"].ToString() ?? Email;

                                // 1. Crear las credenciales de sesión (Claims)
                                var claims = new List<Claim>
                                {
                                    new Claim(ClaimTypes.Name, userEmail),
                                    new Claim(ClaimTypes.NameIdentifier, reader["Id"].ToString() ?? "")
                                };

                                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                                var authProperties = new AuthenticationProperties
                                {
                                    IsPersistent = true
                                };

                                // 2. Guardar la cookie de autenticación en la sesión del navegador
                                await HttpContext.SignInAsync(
                                    CookieAuthenticationDefaults.AuthenticationScheme,
                                    new ClaimsPrincipal(claimsIdentity),
                                    authProperties);

                                return RedirectToPage("/Carnet");
                            }
                            else
                            {
                                MensajeError = "Correo o contraseña incorrectos.";
                                return Page();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MensajeError = $"Error al iniciar sesión: {ex.Message}";
                return Page();
            }
        }

        // 3. Handler invocado por el botón "Cerrar Sesión" de la barra de navegación
        public async Task<IActionResult> OnPostLogoutAsync()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToPage("/Index");
        }
    }
}