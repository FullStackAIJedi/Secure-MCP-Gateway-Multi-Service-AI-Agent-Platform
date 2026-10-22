using Microsoft.EntityFrameworkCore;

using TaskService.Api.Data;
using TaskService.Api.Tools;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<TaskDbContext>("taskdb");

builder.Services.AddMcpServer()
    .WithHttpTransport(options =>
    {
        options.Stateless = true;   
    })
    .WithTools<TaskTools>();   

var app = builder.Build();
 
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TaskDbContext>();
    await db.Database.MigrateAsync();
}

app.MapHealthChecks("/health");
app.MapMcp("/mcp");

app.Run();

public partial class Program { }