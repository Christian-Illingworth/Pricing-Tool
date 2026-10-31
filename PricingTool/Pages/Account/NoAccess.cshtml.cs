using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PricingTool.Pages.Account;

// Where refused requests are sent (FR-001). It explains what access needs and shows no data.
[AllowAnonymous]
public class NoAccessModel : PageModel
{
    public bool IsSignedIn => User.Identity?.IsAuthenticated == true;

    public void OnGet()
    {
    }
}
