namespace Gateway.Api.Tools
{
    using System.ComponentModel;
    using System.Diagnostics;

    using Gateway.Core.Abstractions;

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

        public GatewayTools(
            IHttpClientFactory httpClientFactory,
            IToolPermissionPolicy permissionPolicy,
            IToolAuditLogger auditLogger,
            ILogger<GatewayTools> logger)
        {
            _httpClientFactory = httpClientFactory;
            _permissionPolicy = permissionPolicy;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        [McpServerTool, Description("Lists all tasks")]
        public async Task<string> ListTasks(
            [Description("Subject identifier from token")] string subjectId,
            CancellationToken cancellationToken)
        {
            return await InvokeDownstreamAsync(
                "list_tasks", "taskservice", subjectId,
                async client => await client.CallToolAsync("list_tasks", cancellationToken: cancellationToken),
                cancellationToken);
        }

        [McpServerTool, Description("Creates a new task")]
        public async Task<string> CreateTask(
            [Description("Subject identifier from token")] string subjectId,
            [Description("Task title")] string title,
            [Description("Task description")] string description,
            CancellationToken cancellationToken)
        {
            return await InvokeDownstreamAsync(
                "create_task", "taskservice", subjectId,
                async client => await client.CallToolAsync("create_task",
                    new Dictionary<string, object?> { ["title"] = title, ["description"] = description },
                    cancellationToken: cancellationToken),
                cancellationToken);
        }

        [McpServerTool, Description("Marks a task as complete")]
        public async Task<string> CompleteTask(
            [Description("Subject identifier from token")] string subjectId,
            [Description("Task ID")] int id,
            CancellationToken cancellationToken)
        {
            return await InvokeDownstreamAsync(
                "complete_task", "taskservice", subjectId,
                async client => await client.CallToolAsync("complete_task",
                    new Dictionary<string, object?> { ["id"] = id },
                    cancellationToken: cancellationToken),
                cancellationToken);
        }

        [McpServerTool, Description("Searches documents")]
        public async Task<string> SearchDocuments(
            [Description("Subject identifier from token")] string subjectId,
            [Description("Keyword")] string keyword,
            CancellationToken cancellationToken)
        {
            return await InvokeDownstreamAsync(
                "search_documents", "documentservice", subjectId,
                async client => await client.CallToolAsync("search_documents",
                    new Dictionary<string, object?> { ["keyword"] = keyword },
                    cancellationToken: cancellationToken),
                cancellationToken);
        }

        private async Task<string> InvokeDownstreamAsync(
            string toolName,
            string serviceName,
            string subjectId,
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