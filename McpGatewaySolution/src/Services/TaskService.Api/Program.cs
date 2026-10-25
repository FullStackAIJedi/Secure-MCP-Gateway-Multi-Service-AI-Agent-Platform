////using Microsoft.EntityFrameworkCore; 

////using Serilog;

////using TaskService.Api.Data;
////using TaskService.Api.Tools;


////var builder = WebApplication.CreateBuilder(args);

////builder.AddServiceDefaults();
////builder.AddNpgsqlDbContext<TaskDbContext>("taskdb");

////builder.Services.AddMcpServer()
////    .WithHttpTransport(options =>
////    {
////        options.Stateless = true;   
////    })
////    .WithTools<TaskTools>();   

////var app = builder.Build();

////app.UseExceptionHandler();
////app.UseStatusCodePages();

////app.UseSerilogRequestLogging();

////using (var scope = app.Services.CreateScope())
////{
////    var db = scope.ServiceProvider.GetRequiredService<TaskDbContext>();
////    await db.Database.MigrateAsync();

////    if (app.Environment.IsDevelopment())
////    {
////        await TaskDbSeeder.SeedAsync(db);
////    }
////}

////app.MapDefaultEndpoints();

////builder.Services.AddHealthChecks()
////    .AddNpgSql(builder.Configuration.GetConnectionString("taskdb")!, name: "postgresql", tags: ["ready"]);

////app.MapMcp("/mcp");

////app.Run();

////public partial class Program { }

//using Microsoft.EntityFrameworkCore;

//using TaskService.Api.Data;
//using TaskService.Api.Entities;
//using TaskService.Api.Tools;

//var builder = WebApplication.CreateBuilder(args);

//builder.AddServiceDefaults();

//builder.Services.AddHealthChecks()
//    .AddNpgSql(
//        builder.Configuration.GetConnectionString("taskdb")!,
//        name: "postgresql",
//        tags: ["ready"]);

//builder.AddNpgsqlDbContext<TaskDbContext>("taskdb");

//builder.Services.AddMcpServer()
//    .WithHttpTransport(options =>
//    {
//        options.Stateless = true;
//    })
//    .WithTools<TaskTools>();

//var app = builder.Build();

//using (var scope = app.Services.CreateScope())
//{
//    var db = scope.ServiceProvider.GetRequiredService<TaskDbContext>();
//    await db.Database.MigrateAsync();

//    if (app.Environment.IsDevelopment())
//    {
//        await TaskDbSeeder.SeedAsync(db);
//    }
//}

//app.MapDefaultEndpoints();
//app.MapMcp("/mcp");

//app.Run();

//public partial class Program { }

using Microsoft.EntityFrameworkCore;

using Serilog;

using TaskService.Api.Data;
using TaskService.Api.Entities;
using TaskService.Api.Tools;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("taskdb")!,
        name: "postgresql",
        tags: ["ready"]);

builder.AddNpgsqlDbContext<TaskDbContext>("taskdb");

builder.Services.AddMcpServer()
    .WithHttpTransport(options =>
    {
        options.Stateless = true;
    })
    .WithTools<TaskTools>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseSerilogRequestLogging();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TaskDbContext>();
    await db.Database.MigrateAsync();

    if (app.Environment.IsDevelopment())
    {
        await TaskDbSeeder.SeedAsync(db);
    }
}

app.MapDefaultEndpoints();
app.MapMcp("/mcp");

app.Run();

public partial class Program { }
