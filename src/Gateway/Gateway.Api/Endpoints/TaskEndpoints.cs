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

            group.MapGet("/search", SearchTasks)
                .WithName("SearchTasks");

            group.MapGet("/{id:int}", GetTask)
                .WithName("GetTask");

            group.MapPost("/", CreateTask)
                .RequireAuthorization("mcp.write")
                .WithName("CreateTask");

            group.MapPatch("/{id:int}/complete", CompleteTask)
                .RequireAuthorization("mcp.write")
                .WithName("CompleteTask");

            group.MapPut("/by-title/{currentTitle}", UpdateTaskByTitle)
                .RequireAuthorization("mcp.write")
                .WithName("UpdateTaskByTitle");

            group.MapDelete("/{id:int}", DeleteTask)
                .RequireAuthorization("mcp.admin")
                .WithName("DeleteTask");
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

        private static async Task<IResult> SearchTasks(
            [FromQuery] string keyword,
            [FromServices] GatewayTools tools,
            HttpContext context,
            CancellationToken cancellationToken)
        {
            var subjectId = GetSubjectId(context);
            if (subjectId is null) return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(keyword))
                return Results.Ok(Array.Empty<TaskItemResponse>());

            try
            {
                var json = await tools.SearchTasks(subjectId, keyword, cancellationToken);
                var tasks = JsonSerializer.Deserialize<List<TaskItemResponse>>(json, JsonOptions);
                return Results.Ok(tasks ?? []);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        }

        private static async Task<IResult> GetTask(
            int id,
            [FromServices] GatewayTools tools,
            HttpContext context,
            CancellationToken cancellationToken)
        {
            var subjectId = GetSubjectId(context);
            if (subjectId is null) return Results.Unauthorized();

            try
            {
                var json = await tools.GetTask(subjectId, id, cancellationToken);
                if (string.IsNullOrWhiteSpace(json) || json.Trim() == "null")
                    return Results.NotFound();

                var task = JsonSerializer.Deserialize<TaskItemResponse>(json, JsonOptions);
                return task is null ? Results.NotFound() : Results.Ok(task);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
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
                // TaskTools.CompleteTask връща string, не bool
                var message = await tools.CompleteTask(subjectId, id, cancellationToken);

                if (string.IsNullOrWhiteSpace(message))
                    return Results.NotFound();

                if (message.StartsWith("Error:", StringComparison.Ordinal))
                    return Results.NotFound();

                return Results.Ok(new { success = true, message });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        }

        private static async Task<IResult> UpdateTaskByTitle(
            string currentTitle,
            UpdateTaskRequest request,
            [FromServices] GatewayTools tools,
            HttpContext context,
            CancellationToken cancellationToken)
        {
            var subjectId = GetSubjectId(context);
            if (subjectId is null) return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(currentTitle))
                return Results.BadRequest(new { error = "Current title is required" });

            try
            {
                var message = await tools.UpdateTask(
                    subjectId, currentTitle, request.NewTitle, request.Description, cancellationToken);

                if (string.IsNullOrWhiteSpace(message))
                    return Results.NotFound();

                if (message.StartsWith("Error:", StringComparison.Ordinal))
                    return Results.BadRequest(new { error = message });

                return Results.Ok(new { message });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        }

        private static async Task<IResult> DeleteTask(
            int id,
            [FromServices] GatewayTools tools,
            HttpContext context,
            CancellationToken cancellationToken)
        {
            var subjectId = GetSubjectId(context);
            if (subjectId is null) return Results.Unauthorized();

            try
            { 
                var message = await tools.DeleteTask(subjectId, id, cancellationToken);

                if (string.IsNullOrWhiteSpace(message))
                    return Results.NotFound();

                if (message.StartsWith("Error:", StringComparison.Ordinal))
                    return Results.NotFound();

                return Results.NoContent();
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
