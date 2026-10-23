using Agent.Api.Services;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using OpenAI;
using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Agent.Services;

public sealed class AgentService(
    IConfiguration configuration,
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
        var ollamaUrl = configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
        var model = configuration["Ollama:Model"] ?? "llama3.2";
        var gatewayMcpUrl = configuration["Gateway:McpUrl"] ?? "http://localhost:5000/mcp";

        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri(gatewayMcpUrl),
            AdditionalHeaders = new Dictionary<string, string>
            {
                ["Authorization"] = $"Bearer {jwt}"
            }
        });

        await using var mcpClient = await McpClient.CreateAsync(transport, cancellationToken: cancellationToken);
        var tools = await mcpClient.ListToolsAsync(cancellationToken: cancellationToken);

        _logToolsDiscovered(logger, sessionId, tools.Count, null);

        var openAiClient = new OpenAIClient(
            new System.ClientModel.ApiKeyCredential("ollama"),
            new OpenAIClientOptions
            {
                Endpoint = new Uri($"{ollamaUrl.TrimEnd('/')}/v1")
            });

        IChatClient chatClient = openAiClient.GetChatClient(model).AsIChatClient();

        var toolsDescription = string.Join("\n", tools.Select(t =>
            $"- {t.Name}: {t.Description}"));

        var systemPrompt = $@"You are an AI assistant with access to these tools:
        {toolsDescription}

        RULES:
        1. When the user asks to create a task, respond with ONLY this JSON (no other text):
           {{""tool"": ""create_task"", ""arguments"": {{""title"": ""<title>"", ""description"": ""<description>""}}}}
           ALWAYS include both ""title"" and ""description"". If no description, use "".
        2. When the user asks to see tasks, respond with ONLY:
           {{""tool"": ""list_tasks"", ""arguments"": {{}}}}
        3. When the user asks to complete a task, respond with ONLY:
           {{""tool"": ""complete_task"", ""arguments"": {{""title"": ""<title>""}}}}
        4. When the user asks to search tasks, respond with ONLY:
           {{""tool"": ""search_tasks"", ""arguments"": {{""keyword"": ""...""}}}}
        5. When the user asks to delete a task, respond with ONLY:
           {{""tool"": ""delete_task"", ""arguments"": {{""title"": ""<title>""}}}}
        6. When the user asks to add a document, respond with ONLY:
           {{""tool"": ""add_document"", ""arguments"": {{""title"": ""<title>"", ""content"": ""<content>""}}}}
           ALWAYS include both ""title"" and ""content"". If no content, use "".
        7. When the user asks to search documents, respond with ONLY:
           {{""tool"": ""search_documents"", ""arguments"": {{""keyword"": ""...""}}}}
           ALWAYS include a non-empty ""keyword"". If the user didn't specify a keyword, respond with text asking for one.
        8. When the user asks to get a document by title, respond with ONLY:
           {{""tool"": ""get_document"", ""arguments"": {{""title"": ""<title>""}}}}
           ALWAYS include the exact ""title"" the user mentioned.
           If the user did not provide an exact title but you have a previous tool result from list_documents or search_documents, use the exact title from that result.
           Do NOT say the title has a formatting issue. Just pass the title exactly as it appears.
        9. When the user asks to delete a document by title, respond with ONLY:
           {{""tool"": ""delete_document"", ""arguments"": {{""title"": ""<title>""}}}}
           ALWAYS include the exact ""title"" the user mentioned.
        10. Update document by its current title → {{""tool"": ""update_document"", ""arguments"": {{""currentTitle"": ""<exact current title>"", ""newTitle"": ""<new title or null>"", ""content"": ""<new content or null>""}}}}
            - Use currentTitle from the user's request.
            - If only renaming, set content to null.
            - If only changing content, set newTitle to null.
            - If both, set both.
        11. If no tool matches the user request, respond with text explaining that you cannot help. DO NOT invent a tool.
        12. If the user asks for multiple actions, respond with MULTIPLE JSON objects, one per line.
        13. When the user asks to see ALL documents, respond with ONLY:
            {{""tool"": ""list_documents"", ""arguments"": {{}}}}
            Do NOT use search_documents when the user wants all documents.";

        var history = _sessions.GetOrAdd(sessionId, _ => new List<ChatMessage>
    {
        new(ChatRole.System, systemPrompt)
    });

        history.Add(new ChatMessage(ChatRole.User, message));

        var response = await chatClient.GetResponseAsync(
            history,
            new ChatOptions { ModelId = model },
            cancellationToken);

        var reply = response.Text;
        var toolsUsed = new List<string>();
        var results = new List<string>();

        var toolCalls = ParseAllToolCalls(reply);

        if (toolCalls.Count > 0)
        {
            foreach (var (toolName, arguments) in toolCalls)
            {
                var toolResult = await InvokeToolAsync(mcpClient, toolName, arguments, cancellationToken);
                toolsUsed.Add(toolName);
                results.Add(toolResult);
            }

            var combinedResults = string.Join("\n", results);

            if (!string.IsNullOrEmpty(combinedResults) && combinedResults.StartsWith("APPROVAL_REQUIRED:", StringComparison.Ordinal))
            {
                var parts = combinedResults.Split(':', 3);
                var approvalJson = JsonSerializer.Serialize(new
                {
                    requiresApproval = true,
                    toolName = parts[1],
                    arguments = parts[2]
                });

                history.Add(new ChatMessage(ChatRole.Assistant, reply));
                history.Add(new ChatMessage(ChatRole.User, $"Tool results:\n{combinedResults}"));
                _logSessionActivity(logger, sessionId, history.Count, null);

                return (approvalJson, toolsUsed);
            }

            history.Add(new ChatMessage(ChatRole.Assistant, reply));
            history.Add(new ChatMessage(ChatRole.User, $"Tool results:\n{combinedResults}"));

            var finalResponse = await chatClient.GetResponseAsync(
                history,
                new ChatOptions { ModelId = model },
                cancellationToken);

            var finalText = finalResponse.Text;

            if (string.IsNullOrWhiteSpace(finalText) ||
                finalText.TrimStart().StartsWith('{') ||
                finalText.TrimStart().StartsWith('['))
            {
                finalText = combinedResults;
            }
            else if (ParseAllToolCalls(finalText).Count > 0)
            {
                finalText = combinedResults;
            }

            _logSessionActivity(logger, sessionId, history.Count, null);
            return (finalText, toolsUsed);
        }

        _logSessionActivity(logger, sessionId, history.Count, null);
        return (reply, toolsUsed);
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

        var endpoint = toolName switch
        {
            "delete_task" => $"/api/internal/delete-task?id={id}",
            "delete_document" => $"/api/internal/delete-document?id={id}",
            _ => null
        };

        if (endpoint is null)
            return $"Error: unsupported tool '{toolName}' for approval";

        var gatewayUrl = configuration["Gateway:McpUrl"]?.Replace("/mcp", "") ?? "http://localhost:5000";
        var httpClient = new HttpClient { BaseAddress = new Uri(gatewayUrl) };
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        var response = await httpClient.PostAsync(endpoint, null, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            return $"Error: {response.StatusCode} - {error}";
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var resultDoc = JsonDocument.Parse(json);
        return resultDoc.RootElement.GetProperty("result").GetString() ?? "Done";
    }

    private static List<(string ToolName, Dictionary<string, object?> Arguments)> ParseAllToolCalls(string text)
    {
        var results = new List<(string, Dictionary<string, object?>)>();
        var index = 0;

        while (index < text.Length)
        {
            var start = text.IndexOf('{', index);
            if (start < 0) break;

            var depth = 0;
            var end = -1;
            for (int i = start; i < text.Length; i++)
            {
                if (text[i] == '{') depth++;
                else if (text[i] == '}')
                {
                    depth--;
                    if (depth == 0) { end = i; break; }
                }
            }
            if (end < 0) break;

            var json = text.Substring(start, end - start + 1);
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("tool", out var toolProp))
                {
                    var toolName = toolProp.GetString() ?? string.Empty;
                    var arguments = new Dictionary<string, object?>();

                    if (root.TryGetProperty("arguments", out var argsProp))
                    {
                        foreach (var prop in argsProp.EnumerateObject())
                        {
                            arguments[prop.Name] = prop.Value.ValueKind switch
                            {
                                JsonValueKind.String => prop.Value.GetString(),
                                JsonValueKind.Number => prop.Value.GetInt32(),
                                JsonValueKind.True => true,
                                JsonValueKind.False => false,
                                _ => null
                            };
                        }
                    }

                    if (!string.IsNullOrEmpty(toolName))
                        results.Add((toolName, arguments));
                }
            }
            catch { }

            index = end + 1;
        }

        return results;
    } 

    private static async Task<string> InvokeToolAsync(
    McpClient mcpClient, string toolName, Dictionary<string, object?> arguments, CancellationToken ct)
    {
        var argsJson = JsonSerializer.Serialize(arguments);
        Console.WriteLine($"[AGENT_TOOL] Invoking '{toolName}' with arguments: {argsJson}");

        var result = await mcpClient.CallToolAsync(toolName, arguments, cancellationToken: ct);
        var text = result.Content.OfType<TextContentBlock>().FirstOrDefault()?.Text ?? string.Empty;

        Console.WriteLine($"[AGENT_TOOL] '{toolName}' returned (len={text.Length}): {text.Substring(0, Math.Min(300, text.Length))}");

        return text;
    }
}