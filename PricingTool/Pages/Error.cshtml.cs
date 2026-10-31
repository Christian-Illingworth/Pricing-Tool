using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PricingTool.Pages;

// Outside Development, unhandled exceptions land here. The page shows only a reference ID (the
// request's trace ID); the detail goes to the log, never the screen (contracts/ui-routes.md "Errors").
[AllowAnonymous]
[IgnoreAntiforgeryToken]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public class ErrorModel : PageModel
{
    public string ReferenceId { get; private set; } = "";

    public void OnGet() => ReferenceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

    public void OnPost() => OnGet();
}
