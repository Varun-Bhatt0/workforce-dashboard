namespace WorkforceDashboard.Infrastructure;

internal static class SecurityHeaders
{
    // All scripts, styles and fonts are served from this site, so the policy can be strict:
    // no inline script, no third-party origins. The initial data is embedded as application/json,
    // which the browser does not execute.
    private const string ContentSecurityPolicy =
        "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; font-src 'self'; " +
        "connect-src 'self'; object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'";

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) => app.Use(async (context, next) =>
    {
        var h = context.Response.Headers;
        h.ContentSecurityPolicy = ContentSecurityPolicy;
        h.XContentTypeOptions = "nosniff";
        h.XFrameOptions = "DENY";
        h["Referrer-Policy"] = "strict-origin-when-cross-origin";
        h["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        h["Cross-Origin-Opener-Policy"] = "same-origin";
        await next();
    });
}
