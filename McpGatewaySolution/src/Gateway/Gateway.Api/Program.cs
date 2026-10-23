using System.Security.Claims;
using System.Threading.RateLimiting;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication.JwtBearer;

using Gateway.Api.Tools;
using Gateway.Api.Data;
using Gateway.Api.Endpoints;
using Gateway.Api.Services;
using Gateway.Api.Infrastructure;
using Gateway.Api.Configuration;

using Gateway.Core.Abstractions;
using Gateway.Core.Entities;
using Gateway.Core.Policies;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/app/keys"))
    .SetApplicationName("mcp-gateway");

builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services.ConfigureOptions<JwtBearerOptionsSetup>();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("mcp.read", policy => policy.RequireAssertion(context =>
        context.User.HasClaim(c =>
            c.Type == "scope" &&
            c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains("mcp.tools.read"))))
    .AddPolicy("mcp.write", policy => policy.RequireAssertion(context =>
        context.User.HasClaim(c =>
            c.Type == "scope" &&
            c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains("mcp.tools.write"))))
    .AddPolicy("mcp.admin", policy => policy.RequireAssertion(context =>
        context.User.HasClaim(c =>
            c.Type == "scope" &&
            c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains("mcp.tools.admin"))));

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
    options.Cookie.Name = "XSRF-TOKEN";
    options.Cookie.HttpOnly = false;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

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

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("mcp-tools", context =>
    {
        var subjectId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        return RateLimitPartition.GetFixedWindowLimiter(subjectId, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });
    });

    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        await context.HttpContext.Response.WriteAsync("Rate limit exceeded.", ct);
    };
});

builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<ICookieWriter, CookieWriter>();
builder.Services.AddSingleton<IToolAuditLogger, InMemoryToolAuditLogger>();

builder.Services.AddSingleton<IToolRegistry, ToolRegistry>();
builder.Services.AddSingleton<IToolPermissionPolicy, ScopeBasedToolPermissionPolicy>();
builder.Services.AddScoped<IScopeProvider, HttpContextScopeProvider>();

builder.Services.AddHttpContextAccessor();

var taskServiceUrl = builder.Configuration["services:taskservice:http:0"]
    ?? "http://localhost:5001";
var documentServiceUrl = builder.Configuration["services:documentservice:http:0"]
    ?? "http://localhost:5002";

builder.Services.AddHttpClient("taskservice", client =>
    client.BaseAddress = new Uri(taskServiceUrl));
builder.Services.AddHttpClient("documentservice", client =>
    client.BaseAddress = new Uri(documentServiceUrl));

builder.Services.AddScoped<GatewayTools>();

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

builder.AddNpgsqlDbContext<UserDbContext>("userdb");
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();
    await db.Database.MigrateAsync();
}

app.UseCors("Client");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseAntiforgery();

app.MapHealthChecks("/health");
app.MapAuthEndpoints();
app.MapTaskEndpoints();
app.MapDocumentEndpoints();
app.MapGatewayEndpoints();

app.MapMcp("/mcp").RequireAuthorization("mcp.read").RequireRateLimiting("mcp-tools");

app.MapGet("/api/security/audit-logs", (IToolAuditLogger logger) =>
{
    var inMemory = logger as InMemoryToolAuditLogger;
    return Results.Ok(inMemory?.GetRecords() ?? Array.Empty<ToolInvocationRecord>());
}).RequireAuthorization("mcp.admin");

app.MapPost("/api/internal/delete-task", async (
    int id, GatewayTools tools, CancellationToken ct) =>
{
    var result = await tools.DeleteTaskById(id, ct);
    return Results.Ok(new { result });
}).RequireAuthorization("mcp.admin");

app.MapPost("/api/internal/delete-document", async (
    int id, GatewayTools tools, CancellationToken ct) =>
{
    var result = await tools.DeleteDocumentById(id, ct);
    return Results.Ok(new { result });
}).RequireAuthorization("mcp.admin");

app.Run();

public partial class Program { }
