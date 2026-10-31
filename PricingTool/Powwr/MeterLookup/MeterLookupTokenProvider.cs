using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using PricingTool.Options;
using PricingTool.Powwr.MeterLookup.Dtos;

namespace PricingTool.Powwr.MeterLookup;

// OAuth2 client credentials for the Meter Lookup Service (research R5). The token is held on the
// server, cached until 60 s before it expires, and refreshed by one request at a time. It's never
// logged or sent anywhere but POWWR.
public sealed class MeterLookupTokenProvider(HttpClient http, IOptions<MeterLookupOptions> options, TimeProvider time)
{
    private static readonly TimeSpan EarlyRefresh = TimeSpan.FromSeconds(60);

    private readonly SemaphoreSlim _refresh = new(1, 1);
    private string? _token;
    private DateTimeOffset _expires;

    public async Task<string> GetTokenAsync(CancellationToken ct)
    {
        if (IsCurrent()) return _token!;

        await _refresh.WaitAsync(ct);
        try
        {
            if (IsCurrent()) return _token!;

            var settings = options.Value;
            using var response = await http.PostAsync(settings.TokenUrl, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = settings.ClientId ?? "",
                ["client_secret"] = settings.ClientSecret ?? "",
                ["scope"] = $"udgs-lookupservices-meter-api brokerage:{settings.OrganisationPowwrId}",
            }), ct);

            if (response.StatusCode is System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
                throw new MeterLookupCredentialsException((int)response.StatusCode);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"The meter lookup token request answered HTTP {(int)response.StatusCode}.", null, response.StatusCode);

            var token = await response.Content.ReadFromJsonAsync<TokenResponse>(MeterLookupJson.Options, ct);
            if (string.IsNullOrWhiteSpace(token?.AccessToken))
                throw new HttpRequestException("The meter lookup token response had no access token.");

            _token = token.AccessToken;
            _expires = time.GetUtcNow().AddSeconds(token.ExpiresIn ?? 300) - EarlyRefresh;
            return _token;
        }
        finally
        {
            _refresh.Release();
        }
    }

    // After a 401 or 403, so the next call fetches a fresh token.
    public void Invalidate()
    {
        _token = null;
        _expires = DateTimeOffset.MinValue;
    }

    private bool IsCurrent() => _token is not null && time.GetUtcNow() < _expires;
}

// POWWR's token server refused our client ID or secret. The message names the status, never a value.
public sealed class MeterLookupCredentialsException(int status)
    : Exception($"The meter lookup token server rejected our client credentials (HTTP {status}).");
