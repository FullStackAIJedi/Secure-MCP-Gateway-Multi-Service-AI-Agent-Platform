using Microsoft.EntityFrameworkCore;

using DocumentService.Api.Data;
using DocumentService.Api.Tools;
 
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<DocumentDbContext>("documentdb", configureDbContextOptions: options =>
    options.UseNpgsql(npgsql => npgsql.UseVector()));

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<DocumentTools>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DocumentDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.MapHealthChecks("/health");
app.MapMcp("/mcp");

app.Run();