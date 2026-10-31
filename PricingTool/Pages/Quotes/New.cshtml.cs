using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PricingTool.Auth;

namespace PricingTool.Pages.Quotes;

// A placeholder until its user story is built, so the shell's navigation and its access rule
// (contracts/ui-routes.md) are in place from the Foundational phase.
[Authorize(Policy = Policies.CanQuote)]
public class NewModel : PageModel
{
    public void OnGet()
    {
    }
}
