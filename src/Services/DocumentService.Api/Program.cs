using Microsoft.EntityFrameworkCore;

using Serilog;

using DocumentService.Api.Data;
using DocumentService.Api.Tools;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("documentdb")!,
        name: "postgresql",
        tags: ["ready"]);

builder.AddNpgsqlDbContext<DocumentDbContext>("documentdb", configureDbContextOptions: options =>
    options.UseNpgsql(npgsql => npgsql.UseVector()));

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<DocumentTools>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseSerilogRequestLogging();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DocumentDbContext>();
    await db.Database.MigrateAsync();
}

app.MapDefaultEndpoints();
app.MapMcp("/mcp");

app.Run();

public partial class Program { }
