namespace Microsoft.Extensions.Hosting.Security
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;

    public static class SecurityHeadersExtensions
    {
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        {
            return app.Use(async (context, next) =>
            {
                context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                context.Response.Headers["X-Frame-Options"] = "DENY";
                context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
                context.Response.Headers["Referrer-Policy"] = "no-referrer";
                context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
                context.Response.Headers["Content-Security-Policy"] =
                    "default-src 'self'; " +
                    "script-src 'self' 'unsafe-inline'; " +
                    "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
                    "font-src 'self' https://fonts.gstatic.com; " +
                    "img-src 'self' data:; " +
                    "connect-src 'self' http://localhost:5000 http://localhost:5003 http://localhost:5173; " +
                    "frame-ancestors 'none'";

                if (context.Request.IsHttps)
                {
                    context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
                }

                await next();
            });
        }
    }
}