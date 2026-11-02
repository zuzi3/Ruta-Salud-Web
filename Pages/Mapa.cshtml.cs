using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CarnetSaludWeb.Pages
{
    [Authorize]
    public class MapaModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}