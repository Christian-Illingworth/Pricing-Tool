using System.Net;
using System.Net.Http.Headers;
using PricingTool.Powwr.Pricing;
using PricingTool.Tests.Support;

namespace PricingTool.Tests.Powwr;

// Research R6: retry only 429 and 503, honour Retry-After capped at 10 s, at most 2 retries. Never a
// timeout or any other error, because we don't yet know whether live calls are charged (Q1). No network.
public class PowwrRetryHandlerTests
{
    private static readonly FakeTimeProvider Clock = FakeTimeProvider.At("2026-10-31T10:00:00Z");

    private sealed class ScriptedHandler(params Func<HttpResponseMessage>[] responses) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var next = responses[Math.Min(Calls, responses.Length - 1)];
            Calls++;
            return Task.FromResult(next());
        }
    }

    private static (HttpClient Client, ScriptedHandler Inner, List<TimeSpan> Delays) Build(params Func<HttpResponseMessage>[] responses)
    {
        var inner = new ScriptedHandler(responses);
        var delays = new List<TimeSpan>();
        var handler = new PowwrRetryHandler(Clock, (delay, _) => { delays.Add(delay); return Task.CompletedTask; }) { InnerHandler = inner };
        return (new HttpClient(handler) { BaseAddress = new Uri("https://powwr.test/") }, inner, delays);
    }

    private static Func<HttpResponseMessage> Status(HttpStatusCode code, Action<HttpResponseMessage>? change = null) => () =>
    {
        var response = new HttpResponseMessage(code) { Content = new StringContent("{}") };
        change?.Invoke(response);
        return response;
    };

    private static Task<HttpResponseMessage> Post(HttpClient client) =>
        client.PostAsync("electricprices", new StringContent("{\"quoteDetails\":{}}"));

    [Fact]
    public async Task RateLimitedThenOkIsRetriedOnce()
    {
        var (client, inner, delays) = Build(Status(HttpStatusCode.TooManyRequests), Status(HttpStatusCode.OK));

        var response = await Post(client);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, inner.Calls);
        Assert.Equal([PowwrRetryHandler.DefaultDelay], delays);
    }

    [Fact]
    public async Task ServiceUnavailableHonoursRetryAfterSeconds()
    {
        var (client, _, delays) = Build(
            Status(HttpStatusCode.ServiceUnavailable, r => r.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(3))),
            Status(HttpStatusCode.OK));

        await Post(client);

        Assert.Equal([TimeSpan.FromSeconds(3)], delays);
    }

    [Fact]
    public async Task RetryAfterDateIsMeasuredFromNow()
    {
        var (client, _, delays) = Build(
            Status(HttpStatusCode.TooManyRequests, r => r.Headers.RetryAfter = new RetryConditionHeaderValue(Clock.GetUtcNow().AddSeconds(5))),
            Status(HttpStatusCode.OK));

        await Post(client);

        Assert.Equal([TimeSpan.FromSeconds(5)], delays);
    }

    [Fact]
    public async Task RetryAfterIsCappedAtTenSeconds()
    {
        var (client, _, delays) = Build(
            Status(HttpStatusCode.TooManyRequests, r => r.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromMinutes(5))),
            Status(HttpStatusCode.OK));

        await Post(client);

        Assert.Equal([TimeSpan.FromSeconds(10)], delays);
    }

    [Fact]
    public async Task AtMostTwoRetries()
    {
        var (client, inner, delays) = Build(Status(HttpStatusCode.TooManyRequests));

        var response = await Post(client);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Equal(3, inner.Calls);
        Assert.Equal(2, delays.Count);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    public async Task OtherStatusesAreNotRetried(HttpStatusCode code)
    {
        var (client, inner, delays) = Build(Status(code), Status(HttpStatusCode.OK));

        var response = await Post(client);

        Assert.Equal(code, response.StatusCode);
        Assert.Equal(1, inner.Calls);
        Assert.Empty(delays);
    }

    [Fact]
    public async Task TimeoutsAreNotRetried()
    {
        var (client, inner, _) = Build(() => throw new TaskCanceledException("timed out"));

        await Assert.ThrowsAsync<TaskCanceledException>(() => Post(client));
        Assert.Equal(1, inner.Calls);
    }
}
