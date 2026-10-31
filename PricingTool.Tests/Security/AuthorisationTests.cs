using System.Net;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PricingTool.Auth;
using PricingTool.Infrastructure;
using PricingTool.Tests.Support;

namespace PricingTool.Tests.Security;

// FR-001, FR-002, FR-093, FR-094: roles and the group are enforced on the server, and sessions end
// after eight hours. Run against both sign-in modes.
public class AuthorisationTests
{
    public static TheoryData<string> Modes() => ["Production", "Development"];

    private static PricingToolFactory FactoryFor(string mode) =>
        mode == "Production" ? PricingToolFactory.Production() : PricingToolFactory.Development();

    private static HttpRequestMessage Get(string path, string? roles = null, bool inGroup = true)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (roles is not null) request.Headers.Add("TestUser", roles);
        if (inGroup) request.Headers.Add("TestGroups", PricingToolFactory.AllowedGroupId);
        return request;
    }

    private static string Location(HttpResponseMessage response) => response.Headers.Location?.OriginalString ?? "";

    [Theory]
    [MemberData(nameof(Modes))]
    public async Task AnonymousRequestIsSentToSignIn(string mode)
    {
        using var factory = FactoryFor(mode);
        using var client = factory.Client();

        var response = await client.SendAsync(Get("/Quotes"));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("https://localhost/Account/SignIn?returnUrl=", Location(response));
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public async Task PersonWithNoRoleIsSentToNoAccess(string mode)
    {
        using var factory = FactoryFor(mode);
        using var client = factory.Client();

        foreach (var path in new[] { "/", "/Quotes", "/Quotes/New", "/Reports", "/Settings" })
        {
            var response = await client.SendAsync(Get(path, roles: "none"));
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.StartsWith("https://localhost/Account/NoAccess", Location(response));
        }
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public async Task AdminOnlyCannotOpenNewQuote(string mode)
    {
        using var factory = FactoryFor(mode);
        using var client = factory.Client();

        var response = await client.SendAsync(Get("/Quotes/New", roles: RoleNames.Admin));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("https://localhost/Account/NoAccess", Location(response));
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public async Task AgentCannotOpenAdminPages(string mode)
    {
        using var factory = FactoryFor(mode);
        using var client = factory.Client();

        foreach (var path in new[] { "/Reports", "/Settings" })
        {
            var response = await client.SendAsync(Get(path, roles: RoleNames.Agent));
            Assert.StartsWith("https://localhost/Account/NoAccess", Location(response));
        }
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public async Task EachRoleReachesItsOwnPages(string mode)
    {
        using var factory = FactoryFor(mode);
        using var client = factory.Client();

        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(Get("/Quotes/New", roles: RoleNames.Agent))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(Get("/Quotes", roles: RoleNames.Agent))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(Get("/Reports", roles: RoleNames.Admin))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(Get("/Settings", roles: RoleNames.Admin))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(Get("/Quotes/New", roles: "Agent,Admin"))).StatusCode);
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public async Task StartPageSendsEachRoleToTheRightPlace(string mode)
    {
        using var factory = FactoryFor(mode);
        using var client = factory.Client();

        Assert.Equal("/Quotes/New", Location(await client.SendAsync(Get("/", roles: RoleNames.Agent))));
        Assert.Equal("/Reports", Location(await client.SendAsync(Get("/", roles: RoleNames.Admin))));
    }

    [Fact]
    public async Task EntraUserOutsideTheGroupIsRefusedEvenWithARole()
    {
        using var factory = PricingToolFactory.Production();
        using var client = factory.Client();

        var response = await client.SendAsync(Get("/Quotes/New", roles: RoleNames.Agent, inGroup: false));

        Assert.StartsWith("https://localhost/Account/NoAccess", Location(response));
    }

    [Fact]
    public async Task NavigationShowsOnlyThePagesARoleCanUse()
    {
        using var factory = PricingToolFactory.Development();
        using var client = factory.Client();

        var agentPage = await (await client.SendAsync(Get("/Quotes", roles: RoleNames.Agent))).Content.ReadAsStringAsync();
        Assert.Contains("New quote", agentPage);
        Assert.DoesNotContain("href=\"/Settings\"", agentPage);
        Assert.DoesNotContain("href=\"/Reports\"", agentPage);

        var adminPage = await (await client.SendAsync(Get("/Quotes", roles: RoleNames.Admin))).Content.ReadAsStringAsync();
        Assert.DoesNotContain("href=\"/Quotes/New\"", adminPage);
        Assert.Contains("href=\"/Settings\"", adminPage);
        Assert.Contains("href=\"/Reports\"", adminPage);
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public void SessionCookieLastsEightHoursWithNoSlidingRenewal(string mode)
    {
        using var factory = FactoryFor(mode);
        _ = factory.Services; // starts the host, so options validation runs
        var scheme = mode == "Production" ? CookieAuthenticationDefaults.AuthenticationScheme : DevAuthentication.Scheme;

        var cookie = factory.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(scheme);

        Assert.Equal(TimeSpan.FromHours(8), cookie.ExpireTimeSpan);
        Assert.False(cookie.SlidingExpiration);
        Assert.Equal("/Account/NoAccess", cookie.AccessDeniedPath);
    }

    [Fact]
    public async Task RenderedPagesHaveNoUnresolvedAppRelativeUrls()
    {
        using var factory = PricingToolFactory.Development();
        using var client = factory.Client();

        foreach (var (path, roles) in new[] { ("/Account/SignIn", (string?)null), ("/Dev/SignIn", null), ("/Quotes", RoleNames.Agent) })
        {
            var html = await (await client.SendAsync(Get(path, roles))).Content.ReadAsStringAsync();
            Assert.DoesNotContain("~/", html);
        }
    }

    [Fact]
    public async Task EveryResponseCarriesTheSecurityHeaders()
    {
        using var factory = PricingToolFactory.Production();
        using var client = factory.Client();

        var response = await client.GetAsync("/Account/NoAccess");

        Assert.Equal(SecurityHeaders.ContentSecurityPolicy, response.Headers.GetValues("Content-Security-Policy").Single());
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("same-origin", response.Headers.GetValues("Referrer-Policy").Single());
    }
}
