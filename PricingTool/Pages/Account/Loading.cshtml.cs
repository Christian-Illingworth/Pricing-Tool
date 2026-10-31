using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using PricingTool.Auth;
using AuthenticationOptions = PricingTool.Options.AuthenticationOptions;

namespace PricingTool.Pages.Account;

// "Taking you to Microsoft sign-in…": app.js presses Continue straight away; with script off the
// button does the same. Continue starts the OpenID Connect challenge, or in Development mode goes to
// the test sign-in.
[AllowAnonymous]
public class LoadingModel(IHostEnvironment env, IOptions<AuthenticationOptions> auth) : PageModel
{
    public string? ReturnUrl { get; private set; }

    public bool IsDevelopmentSignIn => DevAuthentication.IsEnabled(env, auth.Value);

    public void OnGet(string? returnUrl) => ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null;

    public IActionResult OnGetContinue(string? returnUrl)
    {
        var target = Url.IsLocalUrl(returnUrl) ? returnUrl : "/";
        if (IsDevelopmentSignIn)
            return RedirectToPage("/Dev/SignIn", new { returnUrl = target });
        return Challenge(new AuthenticationProperties { RedirectUri = target }, OpenIdConnectDefaults.AuthenticationScheme);
    }
}
