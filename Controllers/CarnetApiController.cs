using CarnetSaludWeb.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarnetSaludWeb.Controllers
{
    [ApiController]
    [Route("api/carnet")]
    public class CarnetApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public CarnetApiController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("{email}")]
        public async Task<IActionResult> GetPorCorreo(string email)
        {
            var usuario = await _userManager.FindByEmailAsync(email);
            if (usuario == null)
            {
                return NotFound(new { mensaje = "Usuario no encontrado" });
            }

            var perfil = await _context.Perfiles.FirstOrDefaultAsync(p => p.UserId == usuario.Id);
            if (perfil == null)
            {
                return NotFound(new { mensaje = "Este usuario aún no completó su perfil" });
            }

            return Ok(new
            {
                nombre = perfil.Nombre,
                carrera = perfil.Carrera,
                correo = usuario.Email
            });
        }
    }
}