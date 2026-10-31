using System.Net;

namespace PricingTool.Powwr.Pricing;

// Retries POWWR calls that were turned away for load: HTTP 429 and 503 only, honouring Retry-After
// (capped at 10 s), at most twice (research R6). Timeouts and other errors are never retried, because
// we don't yet know whether live calls are charged (Q1); the agent can retry instead.
public sealed class PowwrRetryHandler : DelegatingHandler
{
    public const int MaxRetries = 2;
    public static readonly TimeSpan DefaultDelay = TimeSpan.FromSeconds(1);
    public static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds(10);

    private readonly TimeProvider _time;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;

    public PowwrRetryHandler(TimeProvider time) : this(time, (delay, ct) => Task.Delay(delay, time, ct))
    {
    }

    // Tests pass a delay that records instead of waiting.
    internal PowwrRetryHandler(TimeProvider time, Func<TimeSpan, CancellationToken, Task> delay)
    {
        _time = time;
        _delay = delay;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        for (var attempt = 0; ; attempt++)
        {
            var response = await base.SendAsync(request, ct);
            if (attempt >= MaxRetries || !IsRetryable(response.StatusCode)) return response;

            var wait = RetryDelay(response, _time.GetUtcNow());
            response.Dispose();
            await _delay(wait, ct);
        }
    }

    public static bool IsRetryable(HttpStatusCode status) =>
        status is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable;

    public static TimeSpan RetryDelay(HttpResponseMessage response, DateTimeOffset now)
    {
        var retryAfter = response.Headers.RetryAfter;
        var wait = retryAfter?.Delta ?? (retryAfter?.Date is { } date ? date - now : DefaultDelay);
        if (wait < TimeSpan.Zero) wait = TimeSpan.Zero;
        return wait > MaxDelay ? MaxDelay : wait;
    }
}
