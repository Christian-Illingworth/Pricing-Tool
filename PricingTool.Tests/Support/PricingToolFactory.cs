using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AspNetAuthenticationOptions = Microsoft.AspNetCore.Authentication.AuthenticationOptions;

namespace PricingTool.Tests.Support;

// Starts the real app in memory for the sign-in and role tests (research R27). The database is never
// touched: the connection string is a dummy, and no test signs in through a path that saves anything.
// Who is signed in comes from the TestUser header; challenges and refusals still go through the
// app's own cookie scheme, so the redirects under test are the real ones.
public sealed class PricingToolFactory(string environment, IReadOnlyDictionary<string, string?> settings)
    : WebApplicationFactory<Program>
{
    public const string AllowedGroupId = "11111111-2222-3333-4444-555555555555";

    // Entra mode with made-up IDs: enough to start, never enough to reach Microsoft.
    public static readonly IReadOnlyDictionary<string, string?> EntraSettings = new Dictionary<string, string?>
    {
        ["Authentication:Mode"] = "Entra",
        ["AzureAd:TenantId"] = "00000000-0000-0000-0000-00000000aaaa",
        ["AzureAd:ClientId"] = "00000000-0000-0000-0000-00000000bbbb",
        ["AzureAd:ClientSecret"] = "not-a-real-secret",
        ["AzureAd:AllowedGroupId"] = AllowedGroupId,
        ["Outlook:Mode"] = "Graph",
        ["ConnectionStrings:PricingDb"] = "Server=(test);Database=NeverUsed;Trusted_Connection=True",
    };

    public static readonly IReadOnlyDictionary<string, string?> DevelopmentSettings = new Dictionary<string, string?>
    {
        ["Authentication:Mode"] = "Development",
        ["Outlook:Mode"] = "Eml",
        ["ConnectionStrings:PricingDb"] = "Server=(test);Database=NeverUsed;Trusted_Connection=True",
    };

    public static PricingToolFactory Production() => new("Production", EntraSettings);

    public static PricingToolFactory Development() => new("Development", DevelopmentSettings);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        foreach (var (key, value) in settings) builder.UseSetting(key, value);
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, TestUserHandler>(TestUserHandler.SchemeName, _ => { });
            services.PostConfigure<AspNetAuthenticationOptions>(o => o.DefaultAuthenticateScheme = TestUserHandler.SchemeName);
        });
    }

    public HttpClient Client() => CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        BaseAddress = new Uri("https://localhost"),
    });
}

// Header "TestUser: Agent,Admin" signs the request in with those roles ("none" for no role).
// Header "TestGroups" lists group object IDs. No header means anonymous.
public sealed class TestUserHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "TestUser";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("TestUser", out var rolesHeader)) return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new List<Claim>
        {
            new("oid", "0d1a0000-0000-4000-8000-0000000000ff"),
            new("name", "Test Person"),
            new("preferred_username", "test.person@example.com"),
        };
        foreach (var role in rolesHeader.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            if (role != "none") claims.Add(new Claim("roles", role));
        if (Request.Headers.TryGetValue("TestGroups", out var groups))
            foreach (var group in groups.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries))
                claims.Add(new Claim("groups", group));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName, "name", "roles"));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }
}
