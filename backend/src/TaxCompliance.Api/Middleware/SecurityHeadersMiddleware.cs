using Microsoft.Extensions.Options;
using TaxCompliance.Api.Configuration;

namespace TaxCompliance.Api.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate next;
    private readonly SecurityHeadersOptions options;

    public SecurityHeadersMiddleware(RequestDelegate next, IOptions<SecurityHeadersOptions> options)
    {
        this.next = next;
        this.options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "SAMEORIGIN";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

        if (options.EnableHsts && context.Request.IsHttps)
        {
            var hstsValue = $"max-age={options.HstsMaxAgeSeconds}";
            if (options.HstsIncludeSubDomains)
            {
                hstsValue += "; includeSubDomains";
            }

            headers["Strict-Transport-Security"] = hstsValue;
        }

        await next(context);
    }
}
