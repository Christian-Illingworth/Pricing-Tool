using System.Net;
using PricingTool.Tests.Support;

namespace PricingTool.Tests.Security;

// FR-095 and constitution Principle I: the Development test sign-in can't be reached anywhere else.
public class DevSignInTests
{
    [Fact]
    public async Task DevSignInIsNotFoundInProduction()
    {
        using var factory = PricingToolFactory.Production();
        using var client = factory.Client();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/Dev/SignIn")).StatusCode);
    }

    [Fact]
    public async Task DevSignInCannotBePostedToInProduction()
    {
        using var factory = PricingToolFactory.Production();
        using var client = factory.Client();

        var response = await client.PostAsync("/Dev/SignIn", new FormUrlEncodedContent(new Dictionary<string, string> { ["key"] = "ada" }));

        // Refused before the handler runs (no antiforgery token) or by the handler itself; either way, no sign-in.
        Assert.Contains(response.StatusCode, new[] { HttpStatusCode.NotFound, HttpStatusCode.BadRequest });
        Assert.False(response.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public void DevelopmentModeOutsideDevelopmentFailsAtStartup()
    {
        var settings = new Dictionary<string, string?>(PricingToolFactory.EntraSettings) { ["Authentication:Mode"] = "Development" };
        using var factory = new PricingToolFactory("Production", settings);

        var error = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Assert.Contains("Authentication:Mode=Development", error.ToString());
    }

    [Fact]
    public async Task DevSignInListsTheMadeUpPeopleInDevelopment()
    {
        using var factory = PricingToolFactory.Development();
        using var client = factory.Client();

        var response = await client.GetAsync("/Dev/SignIn");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        foreach (var name in new[] { "Alex Agent", "Sam Agent", "Ada Admin", "Bea Both", "Pat Nobody" })
            Assert.Contains(name, html);
    }

    [Fact]
    public async Task SignInLoadingGoesToTheDevSignInInDevelopmentMode()
    {
        using var factory = PricingToolFactory.Development();
        using var client = factory.Client();

        var response = await client.GetAsync("/Account/Loading?handler=Continue&returnUrl=%2FQuotes");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/Dev/SignIn", response.Headers.Location!.OriginalString);
    }
}
