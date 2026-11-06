namespace Gateway.Api.Tools
{
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Security.Claims;
    using System.Text.Json;

    using Gateway.Core.Abstractions;
    using Gateway.Core.Contracts;

    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;

    using ModelContextProtocol.Client;
    using ModelContextProtocol.Protocol;
    using ModelContextProtocol.Server;

    [McpServerToolType]
    public sealed partial class GatewayTools
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IToolPermissionPolicy _permissionPolicy;
        private readonly IToolAuditLogger _auditLogger;
        private readonly ILogger<GatewayTools> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IScopeProvider _scopeProvider;
        private readonly IToolRegistry _toolRegistry;
        private readonly string _environmentName;
        private readonly string _environment;

        public GatewayTools(
            IHttpClientFactory httpClientFactory,
            IToolPermissionPolicy permissionPolicy,
            IToolAuditLogger auditLogger,
            ILogger<GatewayTools> logger,
            IHttpContextAccessor httpContextAccessor,
            IToolRegistry toolRegistry,
            IScopeProvider scopeProvider,
            IHostEnvironment hostEnvironment)
        {
            _httpClientFactory = httpClientFactory;
            _permissionPolicy = permissionPolicy;
            _auditLogger = auditLogger;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _toolRegistry = toolRegistry;
            _scopeProvider = scopeProvider;
            _environmentName = hostEnvironment.ApplicationName;
            _environment = hostEnvironment.EnvironmentName;
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
                    return "Tasks:\n(no tasks)";

                var lines = new List<string> { "Tasks:" };
                var index = 1;
                foreach (var task in tasks.EnumerateArray())
                {
                    var title = task.GetProperty("title").GetString();
                    var isComplete = task.GetProperty("isComplete").GetBoolean();
                    var status = isComplete ? "Done" : "Open";
                    var description = task.TryGetProperty("description", out var desc)
                        ? desc.GetString() ?? ""
                        : "";

                    lines.Add($"{index}. {title} [{status}]");
                    if (!string.IsNullOrWhiteSpace(description))
                        lines.Add($"   Description: {description}");

                    index++;
                }

                return string.Join("\n", lines);
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
            [Description("Task description (optional)")] string? description = null,
            [Description("Legacy alias for 'description'.")] string? content = null,
            CancellationToken cancellationToken = default)
        {
            var subjectId = GetSubjectIdFromContext();
            if (string.IsNullOrEmpty(subjectId))
                return "Error: No subject ID found in token.";

            if (string.IsNullOrWhiteSpace(title))
                return "Error: Task title is required.";

            if (description is null && content is not null)
                description = content;

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

        [McpServerTool(Name = "update_task"), Description(
            "Updates a task by its current title. " +
            "You MUST pass 'currentTitle' (exact existing title, e.g. 'task 2'). " +
            "Pass 'newTitle' to rename, 'description' to change description, or both.")]
                public async Task<string> UpdateTaskForAgent(
            [Description("Current task title (exact match) — required lookup key.")]
            string currentTitle,
            [Description("New task title (optional). Omit to keep current title.")]
            string? newTitle = null,
            [Description("New task description (optional). Omit to keep current description.")]
            string? description = null,
            [Description("Legacy alias for 'description'.")]
            string? content = null,
            CancellationToken cancellationToken = default)
        {
            var subjectId = GetSubjectIdFromContext();
            if (string.IsNullOrEmpty(subjectId))
                return "Error: No subject ID found in token.";

            if (string.IsNullOrWhiteSpace(currentTitle))
                return "Error: Current task title is required.";

            if (description is null && content is not null)
                description = content;

            try
            {
                return await UpdateTask(subjectId, currentTitle, newTitle, description, cancellationToken);
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public async Task<string> UpdateTask(
            string subjectId, string currentTitle, string? newTitle, string? description,
            CancellationToken cancellationToken)
        {
            return await InvokeDownstreamAsync(
                "update_task", "taskservice", subjectId,
                async client => await client.CallToolAsync("update_task",
                    new Dictionary<string, object?>
                    {
                        ["subjectId"] = subjectId,
                        ["currentTitle"] = currentTitle,
                        ["newTitle"] = newTitle,
                        ["description"] = description
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

                return await CompleteTask(subjectId, taskId.Value, cancellationToken);
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
                using var doc = JsonDocument.Parse(json);
                var tasks = doc.RootElement;

                if (tasks.ValueKind != JsonValueKind.Array || tasks.GetArrayLength() == 0)
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

        public async Task<string> DeleteTaskById(
            [Description("Task ID")] int id,
            CancellationToken cancellationToken)
        {
            var subjectId = GetSubjectIdFromContext();
            if (string.IsNullOrEmpty(subjectId))
                return "Error: No subject ID found in token.";

            return await DeleteTask(subjectId, id, cancellationToken);
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
                    return "Documents:\n(no documents)";

                var lines = new List<string> { "Documents:" };
                var index = 1;
                foreach (var document in documents.EnumerateArray())
                {
                    var title = document.GetProperty("title").GetString();
                    var description = document.TryGetProperty("description", out var c)
                        ? c.GetString() ?? ""
                        : "";

                    lines.Add($"{index}. {title}");
                    if (!string.IsNullOrWhiteSpace(description))
                        lines.Add($"   Description: {description}");

                    index++;
                }

                return string.Join("\n", lines);
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

                if (json is null || string.IsNullOrWhiteSpace(json) ||
                    json.Trim().Equals("null", StringComparison.OrdinalIgnoreCase))
                    return $"No document found with title \"{title}\".";

                using var fullDoc = JsonDocument.Parse(json);
                if (fullDoc.RootElement.ValueKind == JsonValueKind.Null)
                    return $"No document found with title \"{title}\".";

                var fullTitle = fullDoc.RootElement.GetProperty("title").GetString() ?? "(no title)";
                var description = fullDoc.RootElement.TryGetProperty("description", out var c)
                    ? c.GetString() ?? ""
                    : "";

                return $"Document: \"{fullTitle}\"\nDescription: {description}";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
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

        [McpServerTool(Name = "create_document"),
            Description("Creates a new document for the current user. Title is required, description is optional.")]
                public async Task<string> CreateDocumentForAgent(
            [Description("Document title")] string title,
            [Description("Document description (optional)")] string? description = null,
            [Description("Legacy alias for 'description'.")] string? content = null,
            CancellationToken cancellationToken = default)
        {
            var subjectId = GetSubjectIdFromContext();
            if (string.IsNullOrEmpty(subjectId))
                return "Error: No subject ID found in token.";

            if (string.IsNullOrWhiteSpace(title))
                return "Error: Document title is required.";

            if (description is null && content is not null)
                description = content;

            var json = await CreateDocument(subjectId, title, description ?? string.Empty, cancellationToken);

            try
            {
                using var doc = JsonDocument.Parse(json);
                var docTitle = doc.RootElement.GetProperty("title").GetString();
                var docDescription = doc.RootElement.TryGetProperty("description", out var c)
                    ? c.GetString() ?? ""
                    : "";

                return $"Document created successfully.\nTitle: {docTitle}\nDescription: {docDescription}";
            }
            catch
            {
                return json;
            }
        }

        public async Task<string> CreateDocument(
            string subjectId, string title, string description,
            CancellationToken cancellationToken)
        {
            return await InvokeDownstreamAsync(
                "create_document", "documentservice", subjectId,
                async client => await client.CallToolAsync("create_document",
                    new Dictionary<string, object?>
                    {
                        ["subjectId"] = subjectId,
                        ["title"] = title,
                        ["description"] = description
                    },
                    cancellationToken: cancellationToken),
                cancellationToken);
        }

        [McpServerTool(Name = "update_document"), Description(
             "Updates a document by its current title. " +
             "You MUST pass 'currentTitle' (exact existing title). " +
             "Pass 'newTitle' to rename, 'description' to change description, or both.")]
                public async Task<string> UpdateDocumentForAgent(
             [Description("Current document title (exact match) — required lookup key.")]
            string currentTitle,
             [Description("New document title (optional). Omit to keep current title.")]
            string? newTitle = null,
             [Description("New document description (optional). Omit to keep current description.")]
            string? description = null,
             [Description("Legacy alias for 'description'.")]
            string? content = null,
             CancellationToken cancellationToken = default)
        {
            var subjectId = GetSubjectIdFromContext();
            if (string.IsNullOrEmpty(subjectId))
                return "Error: No subject ID found in token.";

            if (string.IsNullOrWhiteSpace(currentTitle))
                return "Error: Current document title is required.";

            if (description is null && content is not null)
                description = content;

            try
            {
                return await UpdateDocument(subjectId, currentTitle, newTitle, description, cancellationToken);
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public async Task<string> UpdateDocument(
            string subjectId, string currentTitle, string? newTitle, string? description,
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
                        ["description"] = description
                    },
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

        [McpServerTool(Name = "delete_document"),
            Description("Deletes a document by its title (requires approval)")]
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

        public async Task<string> DeleteDocumentById(
            [Description("Document ID")] int id,
            CancellationToken cancellationToken)
        {
            var subjectId = GetSubjectIdFromContext();
            if (string.IsNullOrEmpty(subjectId))
                return "Error: No subject ID found in token.";

            return await DeleteDocument(subjectId, id, cancellationToken);
        }
         
        // HELPERS  
        private async Task<string> InvokeDownstreamAsync(
            string toolName, string serviceName, string subjectId,
            Func<McpClient, Task<CallToolResult>> invoke,
            CancellationToken cancellationToken)
        {
            var userScopes = _scopeProvider.GetScopes();
            var allowed = await _permissionPolicy.IsAllowedAsync(
                subjectId, toolName, userScopes, cancellationToken);

            var userEmail = _httpContextAccessor.HttpContext?.User
                .FindFirst(ClaimTypes.Email)?.Value;

            _toolRegistry.TryGetScope(toolName, out var toolScope);

            if (!allowed)
            {
                await _auditLogger.LogAsync(new ToolInvocationRecord(
                    subjectId, toolName, serviceName, "denied",
                    DateTimeOffset.UtcNow, 0,
                    TraceId: Activity.Current?.TraceId.ToHexString(),
                    SpanId: Activity.Current?.SpanId.ToHexString(),
                    ServiceName: _environmentName,
                    Environment: _environment,
                    UserEmail: userEmail,
                    ToolScope: toolScope.ToString()),
                    cancellationToken);

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
                    await _auditLogger.LogAsync(new ToolInvocationRecord(
                        subjectId, toolName, serviceName, "error",
                        DateTimeOffset.UtcNow, sw.ElapsedMilliseconds,
                        TraceId: Activity.Current?.TraceId.ToHexString(),
                        SpanId: Activity.Current?.SpanId.ToHexString(),
                        ServiceName: _environmentName,
                        Environment: _environment,
                        UserEmail: userEmail,
                        ToolScope: toolScope.ToString(),
                        ErrorMessage: text),
                        cancellationToken);

                    throw new InvalidOperationException(
                        $"Downstream {serviceName}.{toolName} returned error: {text}");
                }

                await _auditLogger.LogAsync(new ToolInvocationRecord(
                    subjectId, toolName, serviceName, "success",
                    DateTimeOffset.UtcNow, sw.ElapsedMilliseconds,
                    TraceId: Activity.Current?.TraceId.ToHexString(),
                    SpanId: Activity.Current?.SpanId.ToHexString(),
                    ServiceName: _environmentName,
                    Environment: _environment,
                    UserEmail: userEmail,
                    ToolScope: toolScope.ToString()),
                    cancellationToken);

                return text;
            }
            catch (Exception ex)
            {
                sw.Stop();
                LogDownstreamToolFailed(_logger, toolName, serviceName, ex);
                await _auditLogger.LogAsync(new ToolInvocationRecord(
                    subjectId, toolName, serviceName, "error",
                    DateTimeOffset.UtcNow, sw.ElapsedMilliseconds,
                    TraceId: Activity.Current?.TraceId.ToHexString(),
                    SpanId: Activity.Current?.SpanId.ToHexString(),
                    ServiceName: _environmentName,
                    Environment: _environment,
                    UserEmail: userEmail,
                    ToolScope: toolScope.ToString(),
                    ErrorMessage: ex.Message),
                    cancellationToken);
                throw;
            }
        }

        [LoggerMessage(Level = LogLevel.Error, Message = "Downstream tool {Tool} failed on {Service}")]
        private static partial void LogDownstreamToolFailed(ILogger logger, string tool, string service, Exception ex);
    }
}
