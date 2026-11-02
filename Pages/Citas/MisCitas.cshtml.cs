using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CarnetSaludWeb.Pages.Citas
{
    [Authorize]
    public class MisCitasModel : PageModel
    {
        public List<CitaItem> CitasRegistradas { get; set; } = new();

        public void OnGet()
        {
            // Cita de ejemplo precargada para demostración
            CitasRegistradas.Add(new CitaItem
            {
                Especialidad = "Cardiología",
                Fecha = "30/08/2026",
                Hora = "16:45 hrs",
                CentroSalud = "Centro Médico Central",
                Estado = "Confirmada"
            });

            CitasRegistradas.Add(new CitaItem
            {
                Especialidad = "Medicina General",
                Fecha = "05/09/2026",
                Hora = "10:00 hrs",
                CentroSalud = "CESFAM San Rafael",
                Estado = "Pendiente"
            });
        }
    }

    public class CitaItem
    {
        public string Especialidad { get; set; } = string.Empty;
        public string Fecha { get; set; } = string.Empty;
        public string Hora { get; set; } = string.Empty;
        public string CentroSalud { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
    }
}