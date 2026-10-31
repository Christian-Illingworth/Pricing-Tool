using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using PricingTool.Options;
using AuthenticationOptions = PricingTool.Options.AuthenticationOptions;

namespace PricingTool.Pages.Account;

// Signing out is a POST with the antiforgery token, so a link elsewhere can't sign someone out.
// Anyone signed in can do it, including a person with no role.
[Authorize]
public class SignOutModel(IOptions<AuthenticationOptions> auth) : PageModel
{
    public IActionResult OnGet() => LocalRedirect("~/");

    public IActionResult OnPost()
    {
        var afterSignOut = new AuthenticationProperties { RedirectUri = Url.Page("/Account/SignIn") };
        return auth.Value.Mode == AuthenticationMode.Development
            ? SignOut(afterSignOut, Auth.DevAuthentication.Scheme)
            : SignOut(afterSignOut, CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme);
    }
}
