using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Agent.Api.Contracts;
using Agent.Api.Services;
using Agent.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IAgentService, AgentService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Client", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors("Client");

app.MapPost("/api/agent/chat", async (
    ChatRequest request,
    HttpContext context,
    IAgentService agent,
    CancellationToken cancellationToken) =>
{
    var authHeader = context.Request.Headers["Authorization"].ToString();
    var cookieToken = context.Request.Cookies["access_token"];

    var jwt = authHeader.Replace("Bearer ", "").Trim();
    if (string.IsNullOrEmpty(jwt))
        jwt = cookieToken ?? string.Empty;

    if (string.IsNullOrEmpty(jwt))
    { 
        return Results.Unauthorized();
    }

    try
    {
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(jwt);
         
        var subjectId = token.Claims.FirstOrDefault(c =>
            c.Type == "nameid" ||
            c.Type == "sub" ||
            c.Type == ClaimTypes.NameIdentifier ||
            c.Type.EndsWith("nameidentifier", StringComparison.Ordinal))?.Value; 
     }
    catch (Exception ex)
    {
        Console.WriteLine($"[AGENT] JWT parse error: {ex.Message}");
    }

    var (reply, toolsUsed) = await agent.ChatAsync(
        request.SessionId, request.Message, jwt, cancellationToken);

    return Results.Ok(new ChatResponse(reply, toolsUsed));
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();