using Agent.Api.Services;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using OpenAI;
using System.Collections.Concurrent;
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
           {{""tool"": ""create_task"", ""arguments"": {{""title"": ""..."", ""description"": ""...""}}}}
        2. When the user asks to see tasks, respond with ONLY:
           {{""tool"": ""list_tasks"", ""arguments"": {{}}}}
        3. When the user asks to complete a task, respond with ONLY:
           {{""tool"": ""complete_task"", ""arguments"": {{""title"": ""..."", ""description"": ""...""}}}}
        4. When the user asks to search tasks, respond with ONLY:
           {{""tool"": ""search_tasks"", ""arguments"": {{""keyword"": ""...""}}}}
        5. When the user asks to delete a task, respond with ONLY:
           {{""tool"": ""delete_task"", ""arguments"": {{""title"": ""..."", ""description"": ""...""}}}}
        6. When the user asks to search documents, respond with ONLY:
           {{""tool"": ""search_documents"", ""arguments"": {{""keyword"": ""...""}}}}
        7. NEVER simulate. Use the tools. Respond with JSON only when calling a tool.
        8. If the user asks for multiple actions, respond with MULTIPLE JSON objects, one per line.";

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

            history.Add(new ChatMessage(ChatRole.Assistant, reply));
            history.Add(new ChatMessage(ChatRole.User, $"Tool results:\n{string.Join("\n", results)}"));

            var finalResponse = await chatClient.GetResponseAsync(
                history,
                new ChatOptions { ModelId = model },
                cancellationToken);

            var finalText = finalResponse.Text;

            if (ParseAllToolCalls(finalText).Count > 0)
            {
                finalText = string.Join("\n", results);
            }

            _logSessionActivity(logger, sessionId, history.Count, null);
            return (finalText, toolsUsed);
        }

        _logSessionActivity(logger, sessionId, history.Count, null);
        return (reply, toolsUsed);
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
        var result = await mcpClient.CallToolAsync(toolName, arguments, cancellationToken: ct);
        return result.Content.OfType<TextContentBlock>().FirstOrDefault()?.Text ?? string.Empty;
    }
}