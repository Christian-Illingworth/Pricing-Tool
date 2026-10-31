using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PricingTool.Auth;

namespace PricingTool.Pages.Reports;

// A placeholder until its user story is built, so the shell's navigation and its access rule
// (contracts/ui-routes.md) are in place from the Foundational phase.
[Authorize(Policy = Policies.CanAdmin)]
public class IndexModel : PageModel
{
    public void OnGet()
    {
    }
}
