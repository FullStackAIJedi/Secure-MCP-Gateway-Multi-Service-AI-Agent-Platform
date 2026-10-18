namespace Gateway.Api.Infrastructure
{ 
    public interface ICookieWriter
    {
        void WriteAuthCookies(HttpContext context, string accessToken);
        void ClearAuthCookies(HttpContext context);
    }
}
