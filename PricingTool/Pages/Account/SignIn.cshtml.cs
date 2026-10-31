using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using PricingTool.Auth;
using AuthenticationOptions = PricingTool.Options.AuthenticationOptions;

namespace PricingTool.Pages.Account;

// Where anyone not signed in is sent. The button continues to the loading page, which starts the
// Microsoft sign-in (or the Development sign-in).
[AllowAnonymous]
public class SignInModel(IHostEnvironment env, IOptions<AuthenticationOptions> auth) : PageModel
{
    public string? ReturnUrl { get; private set; }

    public bool IsDevelopmentSignIn => DevAuthentication.IsEnabled(env, auth.Value);

    public IActionResult OnGet(string? returnUrl)
    {
        ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null;
        if (User.Identity?.IsAuthenticated == true)
            return LocalRedirect(ReturnUrl ?? "~/");
        return Page();
    }
}
