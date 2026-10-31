using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PricingTool.Domain;
using PricingTool.Options;
using PricingTool.Powwr.Pricing.Dtos;

namespace PricingTool.Powwr.Pricing;

// The real UK Pricing API, for SwaggerHub and Live modes (research R4). A typed HttpClient: the base
// address, timeout and PowwrRetryHandler are set up in PowwrServiceCollectionExtensions. POWWR's own
// errors, timeouts, rate limits and bad responses come back as a failed SitePriceResponse with a safe
// message; the detail goes to the log, without the licence code.
public sealed class PowwrPricingHttpClient(
    HttpClient http,
    IOptions<PowwrPricingOptions> options,
    TimeProvider time,
    ILogger<PowwrPricingHttpClient> log) : IPowwrPricingClient
{
    public const string SwaggerHubBaseUrl = "https://virtserver.swaggerhub.com/POWWR/UkPricingAPI/1.2.0";
    public const string LiveBaseUrl = "https://udcoreapi.co.uk/Service.svc/web";

    // SwaggerHub's mock server takes any licence code (research R4).
    private const string SwaggerHubLicence = "SWAGGERHUB-PLACEHOLDER";

    private const string CredentialsMessage = "Pricing is unavailable. Tell an admin.";

    private string LicenceCode => options.Value.Mode == PowwrPricingMode.SwaggerHub && string.IsNullOrWhiteSpace(options.Value.LicenceCode)
        ? SwaggerHubLicence
        : options.Value.LicenceCode ?? "";

    // SwaggerHub mode uses the virtserver address unless a different BaseUrl is set.
    public static string BaseUrlFor(PowwrPricingOptions options) => options.Mode switch
    {
        PowwrPricingMode.SwaggerHub when string.IsNullOrWhiteSpace(options.BaseUrl) || options.BaseUrl.TrimEnd('/') == LiveBaseUrl => SwaggerHubBaseUrl,
        _ => string.IsNullOrWhiteSpace(options.BaseUrl) ? LiveBaseUrl : options.BaseUrl,
    };

    public Task<SitePriceResponse> GetElectricPricesAsync(ElectricPriceRequest request, CancellationToken ct) =>
        PriceAsync("electricprices", PowwrPricingMapper.ToElectricRequest(request, LicenceCode, options.Value.BusinessTypes), request.QuoteReference,
            body => JsonSerializer.Deserialize<GetElectricRatesResultWrapper>(body, PowwrJson.Options)?.GetElectricRatesResult, ct);

    public Task<SitePriceResponse> GetGasPricesAsync(GasPriceRequest request, CancellationToken ct) =>
        PriceAsync("gasprices", PowwrPricingMapper.ToGasRequest(request, LicenceCode, options.Value.BusinessTypes), request.QuoteReference,
            body => JsonSerializer.Deserialize<GetGasRatesResultWrapper>(body, PowwrJson.Options)?.GetGasRatesResult, ct);

    public async Task<RateStructureResponse> GetRateStructureAsync(RateStructureRequest request, CancellationToken ct)
    {
        try
        {
            using var response = await http.PostAsync("prompts", Json(PowwrPricingMapper.ToPromptsRequest(request, LicenceCode)), ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (IsCredentialsRejection(response.StatusCode))
                return new RateStructureResponse(false, [], [], CredentialsMessage, []);

            var prompts = TryRead(body, b => JsonSerializer.Deserialize<GetNumberOfPromptsResultWrapper>(b, PowwrJson.Options)?.GetNumberOfPromptsResult);
            var result = PowwrPricingMapper.FromPrompts(prompts);
            if (result.UnknownPrompts.Count > 0)
                log.LogWarning("POWWR /prompts returned prompt names we don't recognise for MPAN {Mpan}: {Prompts}", request.MpanCore, result.UnknownPrompts);
            if (!result.Succeeded)
                log.LogWarning("POWWR /prompts failed for MPAN {Mpan} (HTTP {Status}): {Message}", request.MpanCore, (int)response.StatusCode, result.ErrorMessage);
            return result;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            log.LogWarning(ex, "POWWR /prompts couldn't be reached for MPAN {Mpan}", request.MpanCore);
            return new RateStructureResponse(false, [], [], "The rate structure lookup isn't available right now. Choose the registers yourself.", []);
        }
    }

    // Throws when the check can't be made, so the caller can block the PDF with a clear message (R14).
    public async Task<IReadOnlyList<SupplierPriceChange>> GetLatestPriceChangesAsync(string? supplierName, CancellationToken ct)
    {
        using var response = await http.PostAsync("pricechange", Json(PowwrPricingMapper.ToPriceChangeRequest(supplierName, LicenceCode)), ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"POWWR /pricechange answered HTTP {(int)response.StatusCode}.", null, response.StatusCode);
        return PowwrPricingMapper.FromPriceChanges(
            JsonSerializer.Deserialize<GetLatestPriceChangeResultWrapper>(body, PowwrJson.Options)?.GetLatestPriceChangeResult);
    }

    public async Task<bool> PingAsync(CancellationToken ct)
    {
        try
        {
            using var response = await http.GetAsync("ping", ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            var ping = TryRead(body, b => JsonSerializer.Deserialize<PingResultWrapper>(b, PowwrJson.Options)?.PingResult);
            return response.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(ping);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            return false;
        }
    }

    private async Task<SitePriceResponse> PriceAsync<TRequest>(
        string path, TRequest request, string quoteReference, Func<string, QuoteResult?> read, CancellationToken ct)
    {
        var masked = PowwrPricingMapper.SerializeMasked(request);
        var sent = time.GetUtcNow().UtcDateTime;

        try
        {
            using var response = await http.PostAsync(path, Json(request), ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            var received = time.GetUtcNow().UtcDateTime;

            if (IsCredentialsRejection(response.StatusCode))
            {
                log.LogError("POWWR rejected our credentials (HTTP {Status}) for {QuoteReference}", (int)response.StatusCode, quoteReference);
                return Failure(ErrorCategory.CredentialsRejected, CredentialsMessage, masked, body, sent, received);
            }
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                log.LogWarning("POWWR is still rate limiting after retries for {QuoteReference}", quoteReference);
                return Failure(ErrorCategory.RateLimited, "POWWR is busy and turned the request away. Wait a minute, then retry.", masked, body, sent, received);
            }

            var result = TryRead(body, read);
            if (result is null && !response.IsSuccessStatusCode)
            {
                log.LogWarning("POWWR answered HTTP {Status} with no readable body for {QuoteReference}", (int)response.StatusCode, quoteReference);
                return Failure(ErrorCategory.PowwrError, $"POWWR couldn't price this site (HTTP {(int)response.StatusCode}). Try again.", masked, body, sent, received);
            }

            var mapped = PowwrPricingMapper.FromQuoteResult(result);
            foreach (var warning in mapped.Warnings)
                log.LogWarning("POWWR response for {QuoteReference}: {Warning}", quoteReference, warning);
            if (!mapped.Succeeded)
            {
                log.LogWarning("POWWR returned an error for {QuoteReference}: {Message} {Detail}", quoteReference, mapped.ErrorMessage, mapped.ErrorDetail);
                return Failure(mapped.ErrorCategory!.Value,
                    mapped.ErrorCategory == ErrorCategory.CredentialsRejected ? CredentialsMessage : mapped.ErrorMessage!,
                    masked, body, sent, received);
            }

            return new SitePriceResponse
            {
                Succeeded = true,
                PowwrReference = mapped.Reference,
                FooterNotes = mapped.FooterNotes,
                Rates = mapped.Rates,
                NoQuotes = mapped.NoQuotes,
                RequestJsonMasked = masked,
                ResponseJson = body,
                SentUtc = sent,
                ReceivedUtc = received,
            };
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            log.LogWarning(ex, "POWWR timed out for {QuoteReference}", quoteReference);
            return Failure(ErrorCategory.Timeout, "POWWR took too long to answer. Try again.", masked, null, sent, time.GetUtcNow().UtcDateTime);
        }
        catch (HttpRequestException ex)
        {
            log.LogWarning(ex, "POWWR couldn't be reached for {QuoteReference}", quoteReference);
            return Failure(ErrorCategory.Network, "POWWR couldn't be reached. Check the connection, then try again.", masked, null, sent, time.GetUtcNow().UtcDateTime);
        }
    }

    private static SitePriceResponse Failure(ErrorCategory category, string message, string masked, string? body, DateTime sent, DateTime received) => new()
    {
        Succeeded = false,
        ErrorCategory = category,
        ErrorMessage = message,
        RequestJsonMasked = masked,
        ResponseJson = body,
        SentUtc = sent,
        ReceivedUtc = received,
    };

    private static bool IsCredentialsRejection(HttpStatusCode status) =>
        status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;

    private static StringContent Json<T>(T request) =>
        new(PowwrPricingMapper.Serialize(request), Encoding.UTF8, "application/json");

    private static T? TryRead<T>(string body, Func<string, T?> read)
    {
        if (string.IsNullOrWhiteSpace(body)) return default;
        try { return read(body); }
        catch (JsonException) { return default; }
    }
}
