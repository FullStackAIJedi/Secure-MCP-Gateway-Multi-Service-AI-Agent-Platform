namespace Agent.Api.Services.Abstractions; 

public sealed record McpTool(string Name, string Description);

public interface IMcpClient : IAsyncDisposable
{
    Task<IReadOnlyList<McpTool>> ListToolsAsync(CancellationToken cancellationToken);

    Task<string> CallToolAsync(
        string toolName,
        Dictionary<string, object?> arguments,
        CancellationToken cancellationToken);
}
