//using System.IdentityModel.Tokens.Jwt;

//using Agent.Api.Contracts;
//using Agent.Api.Services; 
//using Agent.Services;

//var builder = WebApplication.CreateBuilder(args);

//builder.Services.AddSingleton<IAgentService, AgentService>();

//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("Client", policy =>
//    {
//        policy.WithOrigins("http://localhost:5173")
//            .AllowAnyHeader()
//            .AllowAnyMethod()
//            .AllowCredentials();
//    });
//});

//var app = builder.Build();

//app.UseCors("Client");

//app.MapPost("/api/agent/chat", async (
//    ChatRequest request,
//    HttpContext context,
//    IAgentService agent,
//    CancellationToken cancellationToken) =>
//{
//    var authHeader = context.Request.Headers["Authorization"].ToString();
//    var cookieToken = context.Request.Cookies["access_token"];

//    Console.WriteLine($"[AGENT] AuthHeader present: {!string.IsNullOrEmpty(authHeader)}");
//    Console.WriteLine($"[AGENT] Cookie present: {cookieToken != null}");

//    var jwt = authHeader.Replace("Bearer ", "").Trim();
//    if (string.IsNullOrEmpty(jwt))
//        jwt = cookieToken ?? string.Empty;

//    if (string.IsNullOrEmpty(jwt))
//    {
//        Console.WriteLine("[AGENT] No JWT found");
//        return Results.Unauthorized();
//    }

//    try
//    {
//        var handler = new JwtSecurityTokenHandler();
//        var token = handler.ReadJwtToken(jwt);
//        var subjectId = token.Claims.FirstOrDefault(c => c.Type == "nameid" || c.Type == "sub")?.Value;
//        Console.WriteLine($"[AGENT] JWT subjectId = {subjectId}");
//    }
//    catch (Exception ex)
//    {
//        Console.WriteLine($"[AGENT] JWT parse error: {ex.Message}");
//    }

//    var (reply, toolsUsed) = await agent.ChatAsync(
//        request.SessionId, request.Message, jwt, cancellationToken);

//    return Results.Ok(new ChatResponse(reply, toolsUsed));
//});

//app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

//app.Run();

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

    Console.WriteLine($"[AGENT] AuthHeader present: {!string.IsNullOrEmpty(authHeader)}");
    Console.WriteLine($"[AGENT] Cookie present: {cookieToken != null}");

    var jwt = authHeader.Replace("Bearer ", "").Trim();
    if (string.IsNullOrEmpty(jwt))
        jwt = cookieToken ?? string.Empty;

    if (string.IsNullOrEmpty(jwt))
    {
        Console.WriteLine("[AGENT] No JWT found");
        return Results.Unauthorized();
    }

    try
    {
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(jwt);

        Console.WriteLine("[AGENT] === JWT Claims ===");
        foreach (var claim in token.Claims)
        {
            Console.WriteLine($"[AGENT] {claim.Type} = {claim.Value}");
        }
        Console.WriteLine("[AGENT] ===================");

        var subjectId = token.Claims.FirstOrDefault(c =>
            c.Type == "nameid" ||
            c.Type == "sub" ||
            c.Type == ClaimTypes.NameIdentifier ||
            c.Type.EndsWith("nameidentifier", StringComparison.Ordinal))?.Value;

        Console.WriteLine($"[AGENT] JWT subjectId = {subjectId ?? "(null)"}");
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