namespace Gateway.Api.Endpoints
{
    using System.Security.Claims;
    using System.Text.Json;
 
    using Microsoft.AspNetCore.Mvc;

    using Gateway.Api.Tools;
    using Gateway.Api.Contracts;

    public static class TaskEndpoints
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public static void MapTaskEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/v1/tasks")
                .WithTags("Tasks")
                .RequireAuthorization("mcp.read");

            group.MapGet("/", ListTasks)
                .WithName("ListTasks");

            group.MapPost("/", CreateTask)
                .RequireAuthorization("mcp.write")
                .WithName("CreateTask");

            group.MapPatch("/{id}/complete", CompleteTask)
                .RequireAuthorization("mcp.write")
                .WithName("CompleteTask");
        }
         
        private static async Task<IResult> ListTasks(
            [FromServices] GatewayTools tools,
            HttpContext context,
            CancellationToken cancellationToken)
        {
            var subjectId = GetSubjectId(context);
            if (subjectId is null) return Results.Unauthorized();

            try
            {
                var json = await tools.ListTasks(subjectId, cancellationToken);
                var tasks = JsonSerializer.Deserialize<List<TaskItemResponse>>(json, JsonOptions);
                return Results.Ok(tasks ?? []);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
            catch (Exception ex)
            {
                return Results.Problem($"ListTasks failed: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static async Task<IResult> CreateTask(
            CreateTaskRequest request,
            [FromServices] GatewayTools tools,
            HttpContext context,
            CancellationToken cancellationToken)
        {
            var subjectId = GetSubjectId(context);
            if (subjectId is null) return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(request.Title))
                return Results.BadRequest(new { error = "Title is required" });

            try
            {
                var json = await tools.CreateTask(
                    subjectId, request.Title, request.Description ?? string.Empty, cancellationToken);
                var task = JsonSerializer.Deserialize<TaskItemResponse>(json, JsonOptions);
                return Results.Ok(task);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        }

        private static async Task<IResult> CompleteTask(
            int id,
            [FromServices] GatewayTools tools,
            HttpContext context,
            CancellationToken cancellationToken)
        {
            var subjectId = GetSubjectId(context);
            if (subjectId is null) return Results.Unauthorized();

            try
            {
                var json = await tools.CompleteTask(subjectId, id, cancellationToken);
                var success = JsonSerializer.Deserialize<bool>(json, JsonOptions);
                return Results.Ok(new { success });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        }

        private static string? GetSubjectId(HttpContext context)
        {
            return context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    } 
}
