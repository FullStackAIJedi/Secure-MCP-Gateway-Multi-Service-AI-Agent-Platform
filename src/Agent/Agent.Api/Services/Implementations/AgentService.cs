using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;

using Agent.Api.Observability;
using Agent.Api.Services.Abstractions;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Hosting.Observability;

using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace Agent.Api.Services.Implementations;

public sealed class AgentService(
    IConfiguration configuration,
    IMcpClientFactory mcpFactory,
    IChatClientFactory chatFactory,
    ILogger<AgentService> logger) : IAgentService
{
    private readonly ConcurrentDictionary<string, List<ChatMessage>> _sessions = new();

    private static readonly Action<ILogger, string, int, Exception?> _logSessionActivity =
        LoggerMessage.Define<string, int>(
            LogLevel.Information,
            new EventId(1, "SessionActivity"),
            "Session {SessionId}: {MessageCount} messages");

    private static readonly Action<ILogger, string, int, Exception?> _logToolsDiscovered =
        LoggerMessage.Define<string, int>(
            LogLevel.Information,
            new EventId(2, "ToolsDiscovered"),
            "Session {SessionId}: discovered {ToolCount} tools");

    public async Task<(string Reply, List<string> ToolsUsed)> ChatAsync(
    string sessionId, string message, string jwt, CancellationToken cancellationToken)
    {
        var model = configuration["Ollama:Model"] ?? "llama3.2";

        await using var mcpClient = await mcpFactory.CreateAsync(jwt, cancellationToken);
        var mcpTools = await mcpClient.ListToolsAsync(cancellationToken);

        _logToolsDiscovered(logger, sessionId, mcpTools.Count, null);

        var chatClient = chatFactory.Create(model);

        var chatTools = mcpTools
            .Select(t =>
            {
                var toolName = t.Name;
                return (AITool)AIFunctionFactory.Create(
                    async (IDictionary<string, object?> args, CancellationToken ct) =>
                    {
                        var dict = args is Dictionary<string, object?> d
                            ? d
                            : new Dictionary<string, object?>(args);

                        return await mcpClient.CallToolAsync(toolName, dict, ct);
                    },
                    name: toolName,
                    description: t.Description);
            })
            .ToList();

        var chatOptions = new ChatOptions
        {
            ModelId = model,
            Tools = chatTools,
            ToolMode = ChatToolMode.Auto
        };

        var history = _sessions.GetOrAdd(sessionId, _ => new List<ChatMessage>
        {
            new(ChatRole.System, BuildSystemPrompt())
        });

        history.Add(new ChatMessage(ChatRole.User, message));

        var toolsUsed = new List<string>();
        string? lastToolResult = null;

        const int maxSteps = 5;

        for (var step = 0; step < maxSteps; step++)
        {
            using var chatActivity = AgentTelemetry.Source.StartActivity(
                GenAiAttributes.OperationChat,
                ActivityKind.Client);
            chatActivity?.AddGenAiChatAttributes(model);
            chatActivity?.SetTag(McpAttributes.SessionId, sessionId);

            var chatStopwatch = Stopwatch.StartNew();

            ChatResponse response;
            try
            {   //llm call
                response = await chatClient.GetResponseAsync(
                    history,
                    chatOptions,
                    cancellationToken);

                chatStopwatch.Stop();

                chatActivity?.RecordChatDuration(model, chatStopwatch.Elapsed.TotalMilliseconds);
                chatActivity?.RecordTokenUsage(
                    model,
                    response.Usage?.InputTokenCount is long input ? (int)input : null,
                    response.Usage?.OutputTokenCount is long output ? (int)output : null);
            }
            catch (Exception ex)
            {
                chatStopwatch.Stop();
                chatActivity?.SetError(ex);
                throw;
            }

            var functionCalls = response.Messages
                .SelectMany(m => m.Contents)
                .OfType<FunctionCallContent>()
                .ToList();
             
            if (functionCalls.Count == 0)
            {
                var finalText = lastToolResult ?? response.Text ?? string.Empty;
                _logSessionActivity(logger, sessionId, history.Count, null);
                return (finalText, toolsUsed);
            }

            history.AddRange(response.Messages);

            var toolResults = new List<AIContent>();
            var toolNames = new List<string>();

            foreach (var call in functionCalls)
            {
                var toolName = call.Name;
                var arguments = ParseFunctionArguments(call.Arguments);

                toolsUsed.Add(toolName);
                toolNames.Add(toolName);

                string result;
                try
                {
                    result = await mcpClient.CallToolAsync(
                        toolName, arguments, cancellationToken);
                }
                catch (Exception ex)
                {
                    result = $"Error: {ex.Message}";
                }

                lastToolResult = result;
                toolResults.Add(new FunctionResultContent(call.CallId, result));
            }

            history.Add(new ChatMessage(ChatRole.Tool, toolResults));

            var combined = string.Join("\n", toolResults
                .OfType<FunctionResultContent>()
                .Select(r => r.Result?.ToString() ?? string.Empty));
             
            var hasError = toolResults
                .OfType<FunctionResultContent>()
                .Any(r => r.Result?.ToString()?.StartsWith("Error:", StringComparison.Ordinal) == true);
             
            if (combined.StartsWith("APPROVAL_REQUIRED:", StringComparison.Ordinal))
            {
                var parts = combined.Split(':', 3);
                var approvalJson = JsonSerializer.Serialize(new
                {
                    requiresApproval = true,
                    toolName = parts[1],
                    arguments = parts[2]
                });

                _logSessionActivity(logger, sessionId, history.Count, null);
                return (approvalJson, toolsUsed);
            }
             
            if (IsFinalTool(toolNames) && !hasError)
            {
                _logSessionActivity(logger, sessionId, history.Count, null);
                return (combined, toolsUsed);
            } 
        }

        _logSessionActivity(logger, sessionId, history.Count, null);
        return (lastToolResult ?? "Error: too many steps", toolsUsed);
    }

    public async Task<string> ExecuteApprovedToolAsync(
    string toolName, string arguments, string jwt, CancellationToken cancellationToken)
    {
        using var doc = JsonDocument.Parse(arguments);
        var id = doc.RootElement.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.Number
            ? idProp.GetInt32()
            : (int?)null;

        if (id is null)
            return "Error: missing 'id' in approval arguments";
         
        var (method, endpoint) = toolName switch
        {
            "delete_task" => (HttpMethod.Delete, $"/api/v1/tasks/{id}"),
            "delete_document" => (HttpMethod.Delete, $"/api/v1/documents/{id}"),
            _ => (null, (string?)null)
        };

        if (method is null || endpoint is null)
            return $"Error: unsupported tool '{toolName}' for approval";

        using var approvalActivity = AgentTelemetry.Source.StartActivity(
            GenAiAttributes.OperationExecuteTool,
            ActivityKind.Client);
        approvalActivity?.SetTag(GenAiAttributes.ToolName, toolName);
        approvalActivity?.SetTag(McpAttributes.ToolName, toolName);

        var gatewayUrl = configuration["Gateway:McpUrl"]?.Replace("/mcp", "")
            ?? "http://localhost:5000";

        var httpClient = new HttpClient { BaseAddress = new Uri(gatewayUrl) };
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", jwt);

        try
        {
            using var request = new HttpRequestMessage(method, endpoint);
            var response = await httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                approvalActivity?.SetTag("outcome", "error");
                return $"Error: {response.StatusCode} - {error}";
            }

            approvalActivity?.SetTag("outcome", "success");
            return $"Done!";
        }
        catch (Exception ex)
        {
            approvalActivity?.SetError(ex);
            throw;
        }
    }

    private static Dictionary<string, object?> ParseFunctionArguments(
        IDictionary<string, object?>? args)
    {
        if (args is null) return new Dictionary<string, object?>();

        return new Dictionary<string, object?>(args);
    }

    private static bool IsFinalTool(List<string> toolNames)
    {
        var finalTools = new HashSet<string>(StringComparer.Ordinal)
        {
            "list_tasks", "list_documents",
            "get_task", "get_document",
            "create_task", "create_document",
            "update_task", "update_document",
            "complete_task",
            "delete_task", "delete_document",
            "search_tasks", "search_documents"
        };

        return toolNames.Count > 0 && toolNames.All(t => finalTools.Contains(t));
    }

    private static string BuildSystemPrompt() =>
        """
    /no_think
    You are an AI assistant with access to tools for tasks and documents.

    Use the tools via function calling. Do NOT respond with JSON in text.
    Do NOT reformat tool results. Show them exactly as returned.

    If the user asks something that requires a tool, call it.
    If no tool matches, respond with text explaining you cannot help.
    """;
}
