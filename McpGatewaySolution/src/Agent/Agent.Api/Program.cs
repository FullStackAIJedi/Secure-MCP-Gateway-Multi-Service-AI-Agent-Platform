using Microsoft.Extensions.Hosting.Security;

using Serilog;

using Agent.Api.Contracts;
using Agent.Api.Services.Abstractions;
using Agent.Api.Services.Implementations;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddSingleton<IAgentService, AgentService>();
builder.Services.AddSingleton<IMcpClientFactory, McpClientFactory>();
builder.Services.AddSingleton<IChatClientFactory, ChatClientFactory>();

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

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors("Client");
app.UseSecurityHeaders();
app.UseSerilogRequestLogging();

app.MapDefaultEndpoints();

app.MapPost("/api/v1/agent/chat", async (
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
        return Results.Unauthorized();

    var (reply, toolsUsed) = await agent.ChatAsync(
        request.SessionId, request.Message, jwt, cancellationToken);

    return Results.Ok(new ChatResponse(reply, toolsUsed));
});

app.MapPost("/api/v1/agent/approve", async (
    ApprovalRequest req, IAgentService agent, HttpContext ctx, CancellationToken ct) =>
{
    var jwt = ctx.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
    var result = await agent.ExecuteApprovedToolAsync(req.ToolName, req.Arguments, jwt, ct);
    return Results.Ok(new { result });
});

app.Run();

public partial class Program { }
