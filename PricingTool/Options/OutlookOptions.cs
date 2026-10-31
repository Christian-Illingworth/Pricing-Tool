using Microsoft.Extensions.Options;

namespace PricingTool.Options;

public enum OutlookMode { Graph, Eml }

public sealed class OutlookOptions : IValidatableOptions
{
    public const string Section = "Outlook";

    // Graph: a draft in the agent's mailbox. Eml: the fallback file (research R3).
    public OutlookMode Mode { get; set; } = OutlookMode.Graph;

    public IEnumerable<string> Validate() => [];
}

// The Development sign-in has no Graph token, so it can only use the .eml fallback.
internal sealed class OutlookModeValidator(IOptions<AuthenticationOptions> auth) : IValidateOptions<OutlookOptions>
{
    public ValidateOptionsResult Validate(string? name, OutlookOptions options) =>
        auth.Value.Mode == AuthenticationMode.Development && options.Mode != OutlookMode.Eml
            ? ValidateOptionsResult.Fail("Outlook:Mode must be Eml when Authentication:Mode is Development.")
            : ValidateOptionsResult.Success;
}
