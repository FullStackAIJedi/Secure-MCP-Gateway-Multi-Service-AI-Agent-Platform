namespace Gateway.Api.Endpoints
{
    using Gateway.Api.Data;
    using Gateway.Api.Infrastructure;
    using Gateway.Api.Services;
 
    using Microsoft.AspNetCore.Antiforgery;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using System.Security.Claims;

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

            group.MapPost("/agent-token", (HttpContext context, IAuthService authService) =>
            {
                var subjectId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var email = context.User.FindFirst(ClaimTypes.Email)?.Value;

                if (subjectId is null || email is null)
                    return Results.Unauthorized();

                var authUser = new AuthUserResponse(subjectId, email);
                var token = authService.GenerateTokenFor(authUser, "mcp.tools.read", "mcp.tools.write");
                return Results.Ok(new { accessToken = token });
            }).RequireAuthorization();
        }
         
        private static IResult Register(
            [FromBody] RegisterRequest request,
            [FromServices] IAuthService authService,
            [FromServices] ICookieWriter cookies,
            HttpContext context)
        {
            var user = authService.Register(request);
            if (user is null) return Results.BadRequest(new { error = "Invalid registration data" });

            var token = authService.GenerateTokenFor(user, "mcp.tools.read", "mcp.tools.write");
            if (token is null) return Results.Problem("Token generation failed");

            cookies.WriteAuthCookies(context, token);
            return Results.Ok(new LoginResponse(user));
        }

        private static IResult Login(
            [FromBody] LoginRequest request,
            [FromServices] IAuthService authService,
            [FromServices] ICookieWriter cookies,
            HttpContext context)
        {
            var user = authService.Login(request);
            if (user is null) return Results.Unauthorized();

            var token = authService.GenerateTokenFor(user, "mcp.tools.read", "mcp.tools.write");
            if (token is null) return Results.Problem("Token generation failed");

            cookies.WriteAuthCookies(context, token);
            return Results.Ok(new LoginResponse(user));
        }

        private static IResult AgentToken(
            string? email,
            [FromServices] UserDbContext db,
            [FromServices] IAuthService authService,
            [FromQuery] string? scopes)
        {
            var userEmail = email ?? "sekul.net@gmail.com";
            var user = db.Users.AsNoTracking().FirstOrDefault(u => u.Email == userEmail);
            if (user is null) return Results.NotFound(new { error = "User not found" });

            var requestedScopes = string.IsNullOrEmpty(scopes)
                ? new[] { "mcp.tools.read", "mcp.tools.write" }
                : scopes.Split(',', StringSplitOptions.RemoveEmptyEntries);

            var authUser = new AuthUserResponse(user.Id.ToString(), user.Email);
            var token = authService.GenerateTokenFor(authUser, requestedScopes);
            return Results.Ok(new { accessToken = token });
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
