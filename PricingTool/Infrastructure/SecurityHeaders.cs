namespace PricingTool.Infrastructure;

// Security headers on every response (research R24). The CSP allows only our own scripts and styles,
// so there are no inline scripts or style attributes anywhere; forms may also post to Microsoft
// sign-in. HSTS is added by UseHsts outside Development.
public static class SecurityHeaders
{
    public const string ContentSecurityPolicy =
        "default-src 'self'; img-src 'self' data:; style-src 'self'; script-src 'self'; frame-ancestors 'none'; form-action 'self' https://login.microsoftonline.com";

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                headers.ContentSecurityPolicy = ContentSecurityPolicy;
                headers.XContentTypeOptions = "nosniff";
                headers["Referrer-Policy"] = "same-origin";
                return Task.CompletedTask;
            });
            await next(context);
        });
}
