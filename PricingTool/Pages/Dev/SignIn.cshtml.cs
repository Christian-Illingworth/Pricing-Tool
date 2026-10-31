using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using PricingTool.Auth;
using AuthenticationOptions = PricingTool.Options.AuthenticationOptions;

namespace PricingTool.Pages.Dev;

// The Development test sign-in (FR-095). Outside the Development environment with
// Authentication:Mode=Development, both handlers return 404, as if the page didn't exist.
[AllowAnonymous]
public class SignInModel(
    IHostEnvironment env,
    IOptions<AuthenticationOptions> auth,
    StaffMemberSync sync,
    ILogger<SignInModel> log) : PageModel
{
    public IReadOnlyList<DevAuthentication.DevPerson> People => DevAuthentication.People;

    public string? ReturnUrl { get; private set; }

    public IActionResult OnGet(string? returnUrl)
    {
        if (!DevAuthentication.IsEnabled(env, auth.Value)) return NotFound();
        ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? key, string? returnUrl, CancellationToken ct)
    {
        if (!DevAuthentication.IsEnabled(env, auth.Value)) return NotFound();

        var person = DevAuthentication.People.FirstOrDefault(p => p.Key == key);
        if (person is null) return BadRequest();

        var principal = DevAuthentication.PrincipalFor(person);
        await sync.SyncAsync(principal, ct);
        await HttpContext.SignInAsync(DevAuthentication.Scheme, principal, new AuthenticationProperties { IsPersistent = false });
        log.LogInformation("Signed in {ObjectId} with roles {Roles} (Development test sign-in)",
            person.ObjectId, StaffClaims.RoleLabel(principal));

        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl : "~/");
    }
}
