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
        private readonly IScopeProvider _scopeProvider;

        public GatewayTools(
            IHttpClientFactory httpClientFactory,
            IToolPermissionPolicy permissionPolicy,
            IToolAuditLogger auditLogger,
            ILogger<GatewayTools> logger,
            IHttpContextAccessor httpContextAccessor,
            IScopeProvider scopeProvider)
        {
            _httpClientFactory = httpClientFactory;
            _permissionPolicy = permissionPolicy;
            _auditLogger = auditLogger;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _scopeProvider = scopeProvider;
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
         
        [McpServerTool(Name = "delete_task"), Description("Deletes a task by its title (requires approval)")]
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
                 
                return $"APPROVAL_REQUIRED:delete_task:{{\"id\":{taskId.Value}}}";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
         
        [McpServerTool(Name = "delete_document"), Description("Deletes a document by its title (requires approval)")]
        public async Task<string> DeleteDocumentForAgent(
            [Description("Document title to delete")] string title,
            CancellationToken cancellationToken)
        {
            var subjectId = GetSubjectIdFromContext();
            if (string.IsNullOrEmpty(subjectId))
                return "Error: No subject ID found in token.";

            if (string.IsNullOrWhiteSpace(title))
                return "Error: Document title is required.";

            var searchJson = await SearchDocuments(subjectId, title, cancellationToken);

            try
            {
                using var doc = JsonDocument.Parse(searchJson);
                var documents = doc.RootElement;

                if (documents.ValueKind != JsonValueKind.Array || documents.GetArrayLength() == 0)
                    return $"No document found with title \"{title}\".";

                var firstDoc = documents[0];
                var docId = firstDoc.GetProperty("id").GetInt32();

                return $"APPROVAL_REQUIRED:delete_document:{{\"id\":{docId}}}";
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

        [McpServerTool(Name = "search_documents"), Description("Searches documents by keyword. Keyword is required.")]
        public async Task<string> SearchDocumentsForAgent(
            [Description("Keyword")] string keyword,
            CancellationToken cancellationToken)
        {
            var subjectId = GetSubjectIdFromContext();
            if (string.IsNullOrEmpty(subjectId))
                return "Error: No subject ID found in token.";

            if (string.IsNullOrWhiteSpace(keyword))
                return "Error: keyword is required. Use a specific search term.";

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
                    var id = document.GetProperty("id").GetInt32();
                    var title = document.GetProperty("title").GetString();
                    lines.Add($"- [{id}] {title}");
                }
                return $"Found {lines.Count} document(s) matching \"{keyword}\":\n{string.Join("\n", lines)}";
            }
            catch
            {
                return json;
            }
        }

        [McpServerTool(Name = "list_documents"), Description("Lists all documents in the knowledge base")]
        public async Task<string> ListDocumentsForAgent(CancellationToken cancellationToken)
        {
            var subjectId = GetSubjectIdFromContext();
            if (string.IsNullOrEmpty(subjectId))
                return "Error: No subject ID found in token.";

            var json = await ListDocuments(subjectId, cancellationToken);

            try
            {
                using var doc = JsonDocument.Parse(json);
                var documents = doc.RootElement;

                if (documents.ValueKind != JsonValueKind.Array || documents.GetArrayLength() == 0)
                    return "No documents found.";

                var lines = new List<string>();
                foreach (var document in documents.EnumerateArray())
                {
                    var title = document.GetProperty("title").GetString();
                    var id = document.GetProperty("id").GetInt32();
                    lines.Add($"- [{id}] {title}");
                }
                return $"You have {lines.Count} document(s):\n{string.Join("\n", lines)}";
            }
            catch
            {
                return json;
            }
        }

        public async Task<string> ListDocuments(string subjectId, CancellationToken cancellationToken)
        {
            return await InvokeDownstreamAsync(
                "list_documents", "documentservice", subjectId,
                async client => await client.CallToolAsync("list_documents",
                    new Dictionary<string, object?> { ["subjectId"] = subjectId },
                    cancellationToken: cancellationToken),
                cancellationToken);
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

        //[McpServerTool(Name = "delete_document_by_id"), Description("Deletes a document by its ID (used for approved deletions)")]
        public async Task<string> DeleteDocumentById(
            [Description("Document ID")] int id,
            CancellationToken cancellationToken)
        {
            var subjectId = GetSubjectIdFromContext();
            if (string.IsNullOrEmpty(subjectId))
                return "Error: No subject ID found in token.";

            var json = await DeleteDocument(subjectId, id, cancellationToken);
            var success = JsonSerializer.Deserialize<bool>(json);
            return success ? "Document deleted." : $"Could not delete document {id}.";
        }
         
        public async Task<string> DeleteTaskById(
            [Description("Task ID")] int id,
            CancellationToken cancellationToken)
        {
            var subjectId = GetSubjectIdFromContext();
            if (string.IsNullOrEmpty(subjectId))
                return "Error: No subject ID found in token.";

            var json = await DeleteTask(subjectId, id, cancellationToken);
            var success = JsonSerializer.Deserialize<bool>(json);
            return success ? "Task deleted." : $"Could not delete task {id}.";
        } 
        public async Task<string> DeleteDocument(
            string subjectId, int id, CancellationToken cancellationToken)
        {
            return await InvokeDownstreamAsync(
                "delete_document", "documentservice", subjectId,
                async client => await client.CallToolAsync("delete_document",
                    new Dictionary<string, object?> { ["subjectId"] = subjectId, ["id"] = id },
                    cancellationToken: cancellationToken),
                cancellationToken);
        }

        [McpServerTool(Name = "get_document"), Description("Gets a specific document by its title")]
        public async Task<string> GetDocumentForAgent(
            [Description("Document title")] string title,
            CancellationToken cancellationToken)
        {
            var subjectId = GetSubjectIdFromContext();
            if (string.IsNullOrEmpty(subjectId))
                return "Error: No subject ID found in token.";

            if (string.IsNullOrWhiteSpace(title))
                return "Error: Document title is required.";

            try
            {
                var json = await GetDocument(subjectId, title, cancellationToken);

                if (json is null || string.IsNullOrWhiteSpace(json) || json.Trim().Equals("null", StringComparison.OrdinalIgnoreCase))
                    return $"No document found with title \"{title}\".";

                using var fullDoc = JsonDocument.Parse(json);
                if (fullDoc.RootElement.ValueKind == JsonValueKind.Null)
                    return $"No document found with title \"{title}\".";

                var fullTitle = GetStringIgnoreCase(fullDoc.RootElement, "title") ?? "(no title)";
                var content = GetStringIgnoreCase(fullDoc.RootElement, "content") ?? "";

                return $"- Title: {fullTitle}\n  Content: {content}";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        private static string? GetStringIgnoreCase(JsonElement element, string propertyName)
        {
            if (element.ValueKind != JsonValueKind.Object)
                return null;

            foreach (var prop in element.EnumerateObject())
            {
                if (string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                    return prop.Value.ValueKind == JsonValueKind.String ? prop.Value.GetString() : null;
            }

            return null;
        }

        public async Task<string> GetDocument(
            string subjectId, string title, CancellationToken cancellationToken)
        {
            var result = await InvokeDownstreamAsync(
                "get_document", "documentservice", subjectId,
                async client => await client.CallToolAsync("get_document",
                    new Dictionary<string, object?>
                    {
                        ["subjectId"] = subjectId,
                        ["title"] = title
                    },
                    cancellationToken: cancellationToken),
                cancellationToken);

            return result ?? string.Empty;
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

            await AddDocument(subjectId, title, content, cancellationToken);
            return "Done";
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

        [McpServerTool(Name = "update_document"), Description("Updates a document by its current title. Provide newTitle and/or content.")]
        public async Task<string> UpdateDocumentForAgent(
            [Description("Current document title (exact match)")] string currentTitle,
            [Description("New document title (optional)")] string? newTitle,
            [Description("New document content (optional)")] string? content,
            CancellationToken cancellationToken)
        {
            var subjectId = GetSubjectIdFromContext();
            if (string.IsNullOrEmpty(subjectId))
                return "Error: No subject ID found in token.";

            if (string.IsNullOrWhiteSpace(currentTitle))
                return "Error: Current document title is required.";

            try
            {
                var json = await UpdateDocument(subjectId, currentTitle, newTitle, content, cancellationToken);
                var success = JsonSerializer.Deserialize<bool>(json);
                return success
                    ? "Done"
                    : $"No document found with title \"{currentTitle}\".";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public async Task<string> UpdateDocument(
            string subjectId, string currentTitle, string? newTitle, string? content,
            CancellationToken cancellationToken)
        {
            return await InvokeDownstreamAsync(
                "update_document", "documentservice", subjectId,
                async client => await client.CallToolAsync("update_document",
                    new Dictionary<string, object?>
                    {
                        ["subjectId"] = subjectId,
                        ["currentTitle"] = currentTitle,
                        ["newTitle"] = newTitle,
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
            var userScopes = _scopeProvider.GetScopes();
            var allowed = await _permissionPolicy.IsAllowedAsync(
                subjectId, toolName, userScopes, cancellationToken);

            if (!allowed)
            {
                await _auditLogger.LogAsync(
                    new ToolInvocationRecord(subjectId, toolName, serviceName, "denied",
                        DateTimeOffset.UtcNow, 0), cancellationToken);
                throw new UnauthorizedAccessException(
                    $"Subject {subjectId} not allowed to call {toolName}");
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

                var text = result.Content.OfType<TextContentBlock>().FirstOrDefault()?.Text ?? string.Empty;

                if (result.IsError == true)
                {
                    await _auditLogger.LogAsync(
                        new ToolInvocationRecord(subjectId, toolName, serviceName, "error",
                            DateTimeOffset.UtcNow, sw.ElapsedMilliseconds), cancellationToken);
                    throw new InvalidOperationException(
                        $"Downstream {serviceName}.{toolName} returned error: {text}");
                }

                await _auditLogger.LogAsync(
                    new ToolInvocationRecord(subjectId, toolName, serviceName, "success",
                        DateTimeOffset.UtcNow, sw.ElapsedMilliseconds), cancellationToken);

                return text;
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

        public IReadOnlyList<ToolInvocationRecord> GetRecords() => _records.AsReadOnly();
    }
}