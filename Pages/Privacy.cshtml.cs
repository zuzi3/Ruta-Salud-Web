using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CarnetSaludWeb.Pages
{
    [AllowAnonymous]
    public class PrivacyModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}

