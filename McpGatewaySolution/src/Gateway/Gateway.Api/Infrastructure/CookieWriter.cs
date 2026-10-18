namespace Gateway.Api.Infrastructure
{
    using Microsoft.AspNetCore.Antiforgery; 

    public sealed class CookieWriter(IAntiforgery antiforgery) : ICookieWriter
    {
        private const string AccessTokenCookie = "access_token";
        private const string CsrfCookie = "XSRF-TOKEN";

        public void WriteAuthCookies(HttpContext context, string accessToken)
        {
            context.Response.Cookies.Append(AccessTokenCookie, accessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddMinutes(15),
                Path = "/"
            });

            var tokens = antiforgery.GetAndStoreTokens(context);
            context.Response.Cookies.Append(CsrfCookie, tokens.RequestToken!, new CookieOptions
            {
                HttpOnly = false,
                Secure = false,
                SameSite = SameSiteMode.Strict,
                Path = "/"
            });
        }

        public void ClearAuthCookies(HttpContext context)
        {
            context.Response.Cookies.Delete(AccessTokenCookie, new CookieOptions { Path = "/" });
            context.Response.Cookies.Delete(CsrfCookie, new CookieOptions { Path = "/" });
        }
    }
}
