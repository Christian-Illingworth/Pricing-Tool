using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PricingTool.Domain;
using PricingTool.Options;
using PricingTool.Powwr.MeterLookup.Dtos;
using PricingTool.Services.Settings;

namespace PricingTool.Powwr.MeterLookup;

// The real Meter Lookup Service, for SwaggerHub, Sandbox and Live modes (research R5). A typed
// HttpClient whose base address comes from BaseUrlFor. Every failure becomes a LookupResult the agent
// can act on, so the lookup never blocks quoting (SC-005).
public sealed class MeterLookupHttpClient(
    HttpClient http,
    MeterLookupTokenProvider tokens,
    ISupplierMatcher suppliers,
    IOptions<MeterLookupOptions> options,
    ILogger<MeterLookupHttpClient> log) : IMeterLookupClient
{
    public const string SwaggerHubBaseUrl = "https://virtserver.swaggerhub.com/POWWR/MeterLookup.GasAndElec.V3.Service/2.5.0/";
    public const string SandboxBaseUrl = "https://meterlookupuk-sbx.dev-platform.powwr.net/api/";
    public const string ApplicationId = "CU Pricing Tool";

    public const string UnavailableMessage = "The meter lookup isn't available right now. Enter the details yourself.";
    public const string CredentialsMessage = "The meter lookup isn't available right now. Tell an admin, and enter the details yourself.";

    public static string BaseUrlFor(MeterLookupOptions options) => options.Mode switch
    {
        MeterLookupMode.SwaggerHub => SwaggerHubBaseUrl,
        MeterLookupMode.Sandbox => SandboxBaseUrl,
        _ => options.BaseUrl!.TrimEnd('/') + "/",
    };

    public async Task<LookupResult<IReadOnlyList<LookupAddress>>> SearchAddressesAsync(NormalisedPostcode postcode, string userId, CancellationToken ct)
    {
        var request = new AddressSearchRequest { CallerIdentification = Caller(userId), PostCode = postcode.Value };
        var outcome = await PostAsync("Search/addressSearch", request, ct);
        if (outcome.Failure is { } failure) return Convert<IReadOnlyList<LookupAddress>>(failure);

        var addresses = MeterLookupMapper.MapAddresses(Read<AddressSearchResults>(outcome.Body!));
        return addresses.Count == 0
            ? LookupResult<IReadOnlyList<LookupAddress>>.NotFound($"No addresses found for {postcode.Value}. Check it, or enter the details yourself.")
            : LookupResult<IReadOnlyList<LookupAddress>>.Found(addresses);
    }

    public async Task<LookupResult<LookupMeters>> SearchMetersAsync(MeterSearch search, string userId, CancellationToken ct)
    {
        var request = new MeterSearchRequest
        {
            CallerIdentification = Caller(userId),
            MeterAddressId = search.MeterAddressId,
            Mpan = search.Mpan,
            Mprn = search.Mprn,
        };
        var outcome = await PostAsync("Search/meterSearch", request, ct);
        if (outcome.Failure is { } failure) return Convert<LookupMeters>(failure);

        var meters = MeterLookupMapper.MapMeters(Read<MeterSearchResults>(outcome.Body!), suppliers);
        return meters.Electric.Count + meters.Gas.Count == 0
            ? LookupResult<LookupMeters>.NotFound("No meters found. Check the number, or enter the details yourself.")
            : LookupResult<LookupMeters>.Found(meters);
    }

    private CallerIdentification Caller(string userId) => new()
    {
        OrganisationPowwrId = options.Value.OrganisationPowwrId,
        ApplicationId = ApplicationId,
        UserId = userId,
    };

    private sealed record Outcome(string? Body, (LookupStatus Status, string Summary)? Failure);

    // One retry after clearing the token on a 401 or 403; everything else is answered once.
    private async Task<Outcome> PostAsync<T>(string path, T request, CancellationToken ct)
    {
        try
        {
            for (var attempt = 1; ; attempt++)
            {
                using var message = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(request, options: MeterLookupJson.Options) };
                if (options.Value.Mode != MeterLookupMode.SwaggerHub)
                    message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await tokens.GetTokenAsync(ct));

                using var response = await http.SendAsync(message, ct);
                var body = await response.Content.ReadAsStringAsync(ct);

                if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                {
                    tokens.Invalidate();
                    if (attempt == 1) continue;
                    log.LogError("The meter lookup rejected our credentials (HTTP {Status}) on {Path}", (int)response.StatusCode, path);
                    return new Outcome(null, (LookupStatus.Unavailable, CredentialsMessage));
                }
                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    var (agentMessage, logDetail) = MeterLookupMapper.SummariseProblem(body);
                    log.LogWarning("The meter lookup rejected a request to {Path}: {Detail}", path, logDetail);
                    return new Outcome(null, (LookupStatus.Rejected, agentMessage));
                }
                if (!response.IsSuccessStatusCode)
                {
                    log.LogWarning("The meter lookup answered HTTP {Status} on {Path}", (int)response.StatusCode, path);
                    return new Outcome(null, (LookupStatus.Unavailable, UnavailableMessage));
                }
                return new Outcome(body, null);
            }
        }
        catch (MeterLookupCredentialsException ex)
        {
            log.LogError(ex, "The meter lookup token request failed on {Path}", path);
            return new Outcome(null, (LookupStatus.Unavailable, CredentialsMessage));
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException || (ex is TaskCanceledException && !ct.IsCancellationRequested))
        {
            log.LogWarning(ex, "The meter lookup couldn't be reached on {Path}", path);
            return new Outcome(null, (LookupStatus.Unavailable, UnavailableMessage));
        }
    }

    private static LookupResult<TValue> Convert<TValue>((LookupStatus Status, string Summary) failure) =>
        new(failure.Status, default, failure.Summary);

    private static T? Read<T>(string body) =>
        string.IsNullOrWhiteSpace(body) ? default : JsonSerializer.Deserialize<T>(body, MeterLookupJson.Options);
}
