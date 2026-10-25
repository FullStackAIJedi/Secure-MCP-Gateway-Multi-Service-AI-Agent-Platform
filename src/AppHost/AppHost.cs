var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("mcp-postgres-data")
    .WithPgAdmin();

var taskDb = postgres.AddDatabase("taskdb");
var documentDb = postgres.AddDatabase("documentdb");
var userDb = postgres.AddDatabase("userdb");

var taskService = builder.AddProject<Projects.TaskService_Api>("taskservice")
    .WithReference(taskDb)
    .WithHttpEndpoint(port: 5001, name: "task-mcp");

var documentService = builder.AddProject<Projects.DocumentService_Api>("documentservice")
    .WithReference(documentDb)
    .WithHttpEndpoint(port: 5002, name: "document-mcp");

var gateway = builder.AddProject<Projects.Gateway_Api>("gateway")
    .WithReference(taskService)
    .WithReference(documentService)
    .WithReference(userDb)
    .WithHttpEndpoint(port: 5000, name: "gateway-mcp");

builder.AddViteApp("client", "../Web/Client")
    .WithReference(gateway)
    .WithHttpEndpoint(port: 5173, env: "PORT");

builder.AddProject<Projects.Agent_Api>("agent-api");

builder.Build().Run();
