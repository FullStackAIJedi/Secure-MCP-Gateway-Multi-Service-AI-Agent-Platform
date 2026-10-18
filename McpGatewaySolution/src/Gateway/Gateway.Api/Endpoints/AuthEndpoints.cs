namespace Gateway.Api.Endpoints
{
    using System.Security.Claims;

    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Antiforgery;

    using Gateway.Api.Infrastructure;
    using Gateway.Api.Services; 
 
    public static class AuthEndpoints
    {
        public static void MapAuthEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/auth").WithTags("Auth");

            group.MapPost("/register", Register)
                .AllowAnonymous()
                .WithName("Register");

            group.MapPost("/login", Login)
                .AllowAnonymous()
                .WithName("Login");

            group.MapPost("/logout", Logout)
                .RequireAuthorization()
                .WithName("Logout");

            group.MapGet("/me", Me)
                .RequireAuthorization()
                .WithName("Me");
        }

        private static IResult Register(
            [FromBody] RegisterRequest request,
            IAuthService authService,
            [FromServices] ICookieWriter cookies,
            HttpContext context)
        {
            var user = authService.Register(request);
            if (user is null) return Results.BadRequest(new { error = "Invalid registration data" });

            var token = authService.GenerateTokenFor(user);
            if (token is null) return Results.Problem("Token generation failed");

            cookies.WriteAuthCookies(context, token);
            return Results.Ok(new LoginResponse(user));
        }

        private static IResult Login(
            LoginRequest request,
            IAuthService authService,
            ICookieWriter cookies,
            HttpContext context)
        {
            var user = authService.Login(request);
            if (user is null) return Results.Unauthorized();

            var token = authService.GenerateTokenFor(user);
            if (token is null) return Results.Problem("Token generation failed");

            cookies.WriteAuthCookies(context, token);
            return Results.Ok(new LoginResponse(user));
        }

        private static async Task<IResult> Logout(
            ICookieWriter cookies,
            IAntiforgery antiforgery,
            HttpContext context)
        {
            await antiforgery.ValidateRequestAsync(context);
            cookies.ClearAuthCookies(context);
            return Results.Ok();
        }

        private static IResult Me(HttpContext context)
        {
            var subjectId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = context.User.FindFirst(ClaimTypes.Email)?.Value;
            if (subjectId is null) return Results.Unauthorized();
            return Results.Ok(new AuthUserResponse(subjectId, email ?? string.Empty));
        }
    }
}
