namespace Gateway.Api.Tools
{
    using System.Text.Json;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Security.Claims;
 
    using Microsoft.AspNetCore.Http;

    using ModelContextProtocol.Client;
    using ModelContextProtocol.Protocol;
    using ModelContextProtocol.Server;

    using Gateway.Core.Abstractions;
  
    [McpServerToolType]
    public sealed partial class GatewayTools
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IToolPermissionPolicy _permissionPolicy;
        private readonly IToolAuditLogger _auditLogger;
        private readonly ILogger<GatewayTools> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public GatewayTools(
            IHttpClientFactory httpClientFactory,
            IToolPermissionPolicy permissionPolicy,
            IToolAuditLogger auditLogger,
            ILogger<GatewayTools> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _permissionPolicy = permissionPolicy;
            _auditLogger = auditLogger;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        private string? GetSubjectIdFromContext()
        {
            return _httpContextAccessor.HttpContext?.User
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        [McpServerTool(Name = "list_tasks"), Description("Lists all tasks for the current user")]
        public async Task<string> ListTasksForAgent(CancellationToken cancellationToken)
        {
            var subjectId = GetSubjectIdFromContext();
            if (string.IsNullOrEmpty(subjectId))
                return "Error: No subject ID found in token.";

            var json = await ListTasks(subjectId, cancellationToken);

            try
            {
                using var doc = JsonDocument.Parse(json);
                var tasks = doc.RootElement;

                if (tasks.ValueKind != JsonValueKind.Array || tasks.GetArrayLength() == 0)
                    return "No tasks found.";

                var lines = new List<string>();
                foreach (var task in tasks.EnumerateArray())
                {
                    var id = task.GetProperty("id").GetInt32();
                    var title = task.GetProperty("title").GetString();
                    var isComplete = task.GetProperty("isComplete").GetBoolean();
                    lines.Add($"- [{id}] {title} - {(isComplete ? "Done" : "Open")}");
                }
                return $"You have {lines.Count} task(s):\n{string.Join("\n", lines)}";
            }
            catch
            {
                return json;
            }
        }

        public async Task<string> ListTasks(string subjectId, CancellationToken cancellationToken)
        {
            return await InvokeDownstreamAsync(
                "list_tasks", "taskservice", subjectId,
                async client => await client.CallToolAsync("list_tasks",
                    new Dictionary<string, object?> { ["subjectId"] = subjectId },
                    cancellationToken: cancellationToken),
                cancellationToken);
        }
         
        [McpServerTool(Name = "create_task"), Description("Creates a new task. Title is required.")]
        public async Task<string> CreateTaskForAgent(
            [Description("Task title")] string title,
            [Description("Task description (optional)")] string description,
            CancellationToken cancellationToken)
        {
            var subjectId = GetSubjectIdFromContext();
            if (string.IsNullOrEmpty(subjectId))
                return "Error: No subject ID found in token.";

            if (string.IsNullOrWhiteSpace(title))
                return "Error: Task title is required.";

            var json = await CreateTask(subjectId, title, description ?? string.Empty, cancellationToken);

            try
            {
                using var doc = JsonDocument.Parse(json);
                var taskTitle = doc.RootElement.GetProperty("title").GetString();
                return $"Task created: \"{taskTitle}\"";
            }
            catch
            {
                return json;
            }
        }

        public async Task<string> CreateTask(
            string subjectId, string title, string? description,
            CancellationToken cancellationToken)
        {
            return await InvokeDownstreamAsync(
                "create_task", "taskservice", subjectId,
                async client => await client.CallToolAsync("create_task",
                    new Dictionary<string, object?>
                    {
                        ["subjectId"] = subjectId,
                        ["title"] = title,
                        ["description"] = description ?? string.Empty
                    },
                    cancellationToken: cancellationToken),
                cancellationToken);
        }
         
        [McpServerTool(Name = "complete_task"), Description("Marks a task as complete by its title")]
        public async Task<string> CompleteTaskForAgent(
            [Description("Task title to mark as complete")] string title,
            CancellationToken cancellationToken)
        {
            var subjectId = GetSubjectIdFromContext();
            if (string.IsNullOrEmpty(subjectId))
                return "Error: No subject ID found in token.";

            var listJson = await ListTasks(subjectId, cancellationToken);

            try
            {
                using var doc = JsonDocument.Parse(listJson);
                var tasks = doc.RootElement;

                if (tasks.ValueKind != JsonValueKind.Array)
                    return "Error: Could not read tasks.";

                int? taskId = null;
                foreach (var task in tasks.EnumerateArray())
                {
                    var taskTitle = task.GetProperty("title").GetString();
                    if (string.Equals(taskTitle, title, StringComparison.OrdinalIgnoreCase))
                    {
                        taskId = task.GetProperty("id").GetInt32();
                        break;
                    }
                }

                if (taskId is null)
                    return $"No task found with title \"{title}\".";

                var json = await CompleteTask(subjectId, taskId.Value, cancellationToken);
                var success = JsonSerializer.Deserialize<bool>(json);
                return success
                    ? $"Task \"{title}\" marked as Done."
                    : $"Could not mark task \"{title}\" as Done.";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public async Task<string> CompleteTask(
            string subjectId, int id, CancellationToken cancellationToken)
        {
            return await InvokeDownstreamAsync(
                "complete_task", "taskservice", subjectId,
                async client => await client.CallToolAsync("complete_task",
                    new Dictionary<string, object?> { ["subjectId"] = subjectId, ["id"] = id },
                    cancellationToken: cancellationToken),
                cancellationToken);
        }
         
        [McpServerTool(Name = "get_task"), Description("Gets a task by its title")]
        public async Task<string> GetTaskForAgent(
            [Description("Task title")] string title,
            CancellationToken cancellationToken)
        {
            var subjectId = GetSubjectIdFromContext();
            if (string.IsNullOrEmpty(subjectId))
                return "Error: No subject ID found in token.";

            var listJson = await ListTasks(subjectId, cancellationToken);

            try
            {
                using var doc = JsonDocument.Parse(listJson);
                var tasks = doc.RootElement;

                foreach (var task in tasks.EnumerateArray())
                {
                    var taskTitle = task.GetProperty("title").GetString();
                    if (string.Equals(taskTitle, title, StringComparison.OrdinalIgnoreCase))
                    {
                        var isComplete = task.GetProperty("isComplete").GetBoolean();
                        var description = task.TryGetProperty("description", out var desc) ? desc.GetString() : "";
                        return $"Task: \"{title}\"\nStatus: {(isComplete ? "Done" : "Open")}\nDescription: {description}";
                    }
                }

                return $"No task found with title \"{title}\".";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public async Task<string> GetTask(
            string subjectId, int id, CancellationToken cancellationToken)
        {
            return await InvokeDownstreamAsync(
                "get_task", "taskservice", subjectId,
                async client => await client.CallToolAsync("get_task",
                    new Dictionary<string, object?> { ["subjectId"] = subjectId, ["id"] = id },
                    cancellationToken: cancellationToken),
                cancellationToken);
        }

        [McpServerTool(Name = "search_tasks"), Description("Searches tasks by title keyword. Keyword is required.")]
        public async Task<string> SearchTasksForAgent(
            [Description("Keyword to search in task titles. Must not be empty.")] string keyword,
            CancellationToken cancellationToken)
        {
            var subjectId = GetSubjectIdFromContext();
            if (string.IsNullOrEmpty(subjectId))
                return "Error: No subject ID found in token.";

            if (string.IsNullOrWhiteSpace(keyword))
                return "Error: keyword is required. Use list_tasks to see all tasks.";

            var json = await SearchTasks(subjectId, keyword, cancellationToken);

            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var tasks = doc.RootElement;

                if (tasks.ValueKind != System.Text.Json.JsonValueKind.Array || tasks.GetArrayLength() == 0)
                    return $"No tasks found matching \"{keyword}\".";

                var lines = new List<string>();
                foreach (var task in tasks.EnumerateArray())
                {
                    var title = task.GetProperty("title").GetString();
                    var isComplete = task.GetProperty("isComplete").GetBoolean();
                    lines.Add($"- {title} — {(isComplete ? "Done" : "Open")}");
                }
                return $"Found {lines.Count} task(s) matching \"{keyword}\":\n{string.Join("\n", lines)}";
            }
            catch
            {
                return json;
            }
        }

        public async Task<string> SearchTasks(
            string subjectId, string keyword, CancellationToken cancellationToken)
        {
            return await InvokeDownstreamAsync(
                "search_tasks", "taskservice", subjectId,
                async client => await client.CallToolAsync("search_tasks",
                    new Dictionary<string, object?> { ["subjectId"] = subjectId, ["keyword"] = keyword },
                    cancellationToken: cancellationToken),
                cancellationToken);
        }
         
        [McpServerTool(Name = "delete_task"), Description("Deletes a task by its title")]
        public async Task<string> DeleteTaskForAgent(
            [Description("Task title to delete")] string title,
            CancellationToken cancellationToken)
        {
            var subjectId = GetSubjectIdFromContext();
            if (string.IsNullOrEmpty(subjectId))
                return "Error: No subject ID found in token.";

            var listJson = await ListTasks(subjectId, cancellationToken);

            try
            {
                using var doc = JsonDocument.Parse(listJson);
                var tasks = doc.RootElement;

                if (tasks.ValueKind != JsonValueKind.Array)
                    return "Error: Could not read tasks.";

                int? taskId = null;
                foreach (var task in tasks.EnumerateArray())
                {
                    var taskTitle = task.GetProperty("title").GetString();
                    if (string.Equals(taskTitle, title, StringComparison.OrdinalIgnoreCase))
                    {
                        taskId = task.GetProperty("id").GetInt32();
                        break;
                    }
                }

                if (taskId is null)
                    return $"No task found with title \"{title}\".";

                var json = await DeleteTask(subjectId, taskId.Value, cancellationToken);
                var success = JsonSerializer.Deserialize<bool>(json);
                return success
                    ? $"Task \"{title}\" deleted."
                    : $"Could not delete task \"{title}\".";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public async Task<string> DeleteTask(
            string subjectId, int id, CancellationToken cancellationToken)
        {
            return await InvokeDownstreamAsync(
                "delete_task", "taskservice", subjectId,
                async client => await client.CallToolAsync("delete_task",
                    new Dictionary<string, object?> { ["subjectId"] = subjectId, ["id"] = id },
                    cancellationToken: cancellationToken),
                cancellationToken);
        }

        [McpServerTool(Name = "search_documents"), Description("Searches documents by keyword")]
        public async Task<string> SearchDocumentsForAgent(
            [Description("Keyword")] string keyword,
            CancellationToken cancellationToken)
        {
            var subjectId = GetSubjectIdFromContext();
            if (string.IsNullOrEmpty(subjectId))
                return "Error: No subject ID found in token.";

            var json = await SearchDocuments(subjectId, keyword, cancellationToken);

            try
            {
                using var doc = JsonDocument.Parse(json);
                var documents = doc.RootElement;

                if (documents.ValueKind != JsonValueKind.Array || documents.GetArrayLength() == 0)
                    return $"No documents found matching \"{keyword}\".";

                var lines = new List<string>();
                foreach (var document in documents.EnumerateArray())
                {
                    var title = document.GetProperty("title").GetString();
                    lines.Add($"- {title}");
                }
                return $"Found {lines.Count} document(s) matching \"{keyword}\":\n{string.Join("\n", lines)}";
            }
            catch
            {
                return json;
            }
        }

        public async Task<string> SearchDocuments(
            string subjectId, string keyword, CancellationToken cancellationToken)
        {
            return await InvokeDownstreamAsync(
                "search_documents", "documentservice", subjectId,
                async client => await client.CallToolAsync("search_documents",
                    new Dictionary<string, object?> { ["subjectId"] = subjectId, ["keyword"] = keyword },
                    cancellationToken: cancellationToken),
                cancellationToken);
        }

        [McpServerTool(Name = "get_document"), Description("Gets a specific document by its ID")]
        public async Task<string> GetDocumentForAgent(
            [Description("Document ID")] int id,
            CancellationToken cancellationToken)
        {
            var subjectId = GetSubjectIdFromContext();
            if (string.IsNullOrEmpty(subjectId))
                return "Error: No subject ID found in token.";

            return await GetDocument(subjectId, id, cancellationToken);
        }

        public async Task<string> GetDocument(
            string subjectId, int id, CancellationToken cancellationToken)
        {
            return await InvokeDownstreamAsync(
                "get_document", "documentservice", subjectId,
                async client => await client.CallToolAsync("get_document",
                    new Dictionary<string, object?> { ["subjectId"] = subjectId, ["id"] = id },
                    cancellationToken: cancellationToken),
                cancellationToken);
        }

        [McpServerTool(Name = "add_document"), Description("Adds a new document to the knowledge base")]
        public async Task<string> AddDocumentForAgent(
            [Description("Document title")] string title,
            [Description("Document content")] string content,
            CancellationToken cancellationToken)
        {
            var subjectId = GetSubjectIdFromContext();
            if (string.IsNullOrEmpty(subjectId))
                return "Error: No subject ID found in token.";

            return await AddDocument(subjectId, title, content, cancellationToken);
        }

        public async Task<string> AddDocument(
            string subjectId, string title, string content,
            CancellationToken cancellationToken)
        {
            return await InvokeDownstreamAsync(
                "add_document", "documentservice", subjectId,
                async client => await client.CallToolAsync("add_document",
                    new Dictionary<string, object?>
                    {
                        ["subjectId"] = subjectId,
                        ["title"] = title,
                        ["content"] = content
                    },
                    cancellationToken: cancellationToken),
                cancellationToken);
        }

        private async Task<string> InvokeDownstreamAsync(
            string toolName, string serviceName, string subjectId,
            Func<McpClient, Task<CallToolResult>> invoke,
            CancellationToken cancellationToken)
        {
            var allowed = await _permissionPolicy.IsAllowedAsync(subjectId, toolName, serviceName, cancellationToken);
            if (!allowed)
            {
                await _auditLogger.LogAsync(
                    new ToolInvocationRecord(subjectId, toolName, serviceName, "denied",
                        DateTimeOffset.UtcNow, 0), cancellationToken);
                throw new UnauthorizedAccessException($"Subject {subjectId} not allowed to call {toolName}");
            }

            var httpClient = _httpClientFactory.CreateClient(serviceName);
            var transport = new HttpClientTransport(new HttpClientTransportOptions
            {
                Endpoint = new Uri(httpClient.BaseAddress!, "/mcp"),
                Name = serviceName
            });

            await using var client = await McpClient.CreateAsync(transport, cancellationToken: cancellationToken);

            var sw = Stopwatch.StartNew();
            try
            {
                var result = await invoke(client);
                sw.Stop();
                await _auditLogger.LogAsync(
                    new ToolInvocationRecord(subjectId, toolName, serviceName, "success",
                        DateTimeOffset.UtcNow, sw.ElapsedMilliseconds), cancellationToken);

                return result.Content.OfType<TextContentBlock>().FirstOrDefault()?.Text ?? string.Empty;
            }
            catch (Exception ex)
            {
                sw.Stop();
                LogDownstreamToolFailed(_logger, toolName, serviceName, ex);
                await _auditLogger.LogAsync(
                    new ToolInvocationRecord(subjectId, toolName, serviceName, "error",
                        DateTimeOffset.UtcNow, sw.ElapsedMilliseconds), cancellationToken);
                throw;
            }
        }

        [LoggerMessage(Level = LogLevel.Error, Message = "Downstream tool {Tool} failed on {Service}")]
        private static partial void LogDownstreamToolFailed(ILogger logger, string tool, string service, Exception ex);
    }

    public sealed class InMemoryToolAuditLogger : IToolAuditLogger
    {
        private readonly List<ToolInvocationRecord> _records = [];

        public Task LogAsync(ToolInvocationRecord record, CancellationToken cancellationToken)
        {
            _records.Add(record);
            return Task.CompletedTask;
        }
    }
}
