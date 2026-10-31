using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using PricingTool.Options;

namespace PricingTool.Auth;

// Sign-in for both modes (research R2). Either way, the default challenge goes to our own sign-in
// page, refusals go to /Account/NoAccess, and the session cookie ends after Authentication:SessionHours
// with no sliding renewal, so role and group changes apply at the next sign-in (FR-094).
public static class AuthenticationSetup
{
    public static IServiceCollection AddPricingToolAuthentication(this IServiceCollection services, IConfiguration config, IHostEnvironment env)
    {
        var mode = config.GetValue("Authentication:Mode", AuthenticationMode.Entra);
        var sessionHours = config.GetValue("Authentication:SessionHours", 8);

        if (mode == AuthenticationMode.Development)
        {
            // Checked again by AuthenticationEnvironmentValidator at startup, after all configuration loads.
            if (!env.IsDevelopment())
                throw new InvalidOperationException(
                    $"Authentication:Mode=Development is only allowed in the Development environment, not '{env.EnvironmentName}'.");
            DevAuthentication.Register(services);
        }
        else
        {
            AddEntra(services, config);
        }

        var cookieScheme = CookieSchemeFor(mode);
        services.Configure<CookieAuthenticationOptions>(cookieScheme, options =>
        {
            options.LoginPath = "/Account/SignIn";
            options.AccessDeniedPath = "/Account/NoAccess";
            options.ReturnUrlParameter = "returnUrl";
            options.ExpireTimeSpan = TimeSpan.FromHours(sessionHours);
            options.SlidingExpiration = false;
            options.Cookie.Name = mode == AuthenticationMode.Development ? "cu-pricing-dev" : "cu-pricing";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Events.OnRedirectToLogin = context => Redirect(context, StatusCodes.Status401Unauthorized);
            options.Events.OnRedirectToAccessDenied = context =>
            {
                // FR-098: refused requests are logged, with who and where, never tokens.
                var user = context.HttpContext.User;
                Logger(context.HttpContext).LogWarning(
                    "Access refused to {Path} for {ObjectId} with roles {Roles}",
                    context.Request.Path, StaffClaims.ObjectId(user) ?? "(unknown)", StaffClaims.RoleLabel(user));
                return Redirect(context, StatusCodes.Status403Forbidden);
            };
        });

        services.AddAuthorization(Policies.Configure);
        services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, GroupAndRoleHandler>();
        services.AddHttpContextAccessor();
        services.AddScoped<StaffMemberSync>();
        services.AddScoped<CurrentStaff>();
        return services;
    }

    public static string CookieSchemeFor(AuthenticationMode mode) =>
        mode == AuthenticationMode.Development ? DevAuthentication.Scheme : CookieAuthenticationDefaults.AuthenticationScheme;

    private static void AddEntra(IServiceCollection services, IConfiguration config)
    {
        const string cookies = CookieAuthenticationDefaults.AuthenticationScheme;
        const string oidc = OpenIdConnectDefaults.AuthenticationScheme;

        // Single tenant, from AzureAd:TenantId. Mail.ReadWrite is requested later, incrementally,
        // only when an agent first opens a quote in Outlook (research R3).
        services.AddAuthentication(options =>
            {
                options.DefaultScheme = cookies;
                options.DefaultChallengeScheme = cookies;
                options.DefaultForbidScheme = cookies;
            })
            .AddMicrosoftIdentityWebApp(config.GetSection("AzureAd"), oidc, cookies)
            .EnableTokenAcquisitionToCallDownstreamApi(["User.Read"])
            .AddDistributedTokenCaches();
        services.AddDistributedMemoryCache();

        services.Configure<OpenIdConnectOptions>(oidc, options =>
        {
            var previous = options.Events.OnTokenValidated;
            options.Events.OnTokenValidated = async context =>
            {
                await previous(context);
                if (context.Principal is null) return;
                var sync = context.HttpContext.RequestServices.GetRequiredService<StaffMemberSync>();
                await sync.SyncAsync(context.Principal, context.HttpContext.RequestAborted);
                Logger(context.HttpContext).LogInformation(
                    "Signed in {ObjectId} with roles {Roles}", StaffClaims.ObjectId(context.Principal), StaffClaims.RoleLabel(context.Principal));
            };
        });
    }

    // The cookie handler's own behaviour: a redirect for pages, a status code for app.js's fetch calls.
    private static Task Redirect(RedirectContext<CookieAuthenticationOptions> context, int fetchStatus)
    {
        if (string.Equals(context.Request.Headers.XRequestedWith, "XMLHttpRequest", StringComparison.Ordinal))
        {
            context.Response.Headers.Location = context.RedirectUri;
            context.Response.StatusCode = fetchStatus;
        }
        else
        {
            context.Response.Redirect(context.RedirectUri);
        }
        return Task.CompletedTask;
    }

    private static ILogger Logger(HttpContext context) =>
        context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("PricingTool.Auth");
}
