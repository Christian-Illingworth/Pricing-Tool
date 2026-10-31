using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PricingTool.Auth;

namespace PricingTool.Pages;

// Sends people to the right page for their role (contracts/ui-routes.md): agents to a new quote,
// admins without the Agent role to reports.
public class IndexModel(IAuthorizationService authorization) : PageModel
{
    public async Task<IActionResult> OnGetAsync()
    {
        if ((await authorization.AuthorizeAsync(User, Policies.CanQuote)).Succeeded)
            return RedirectToPage("/Quotes/New");
        if ((await authorization.AuthorizeAsync(User, Policies.CanAdmin)).Succeeded)
            return RedirectToPage("/Reports/Index");
        return RedirectToPage("/Account/NoAccess");
    }
}
