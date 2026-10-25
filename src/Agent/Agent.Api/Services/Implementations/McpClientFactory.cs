namespace Agent.Api.Services.Implementations;

using Agent.Api.Services.Abstractions; 

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol; 

public sealed class McpClientFactory(IConfiguration configuration) : IMcpClientFactory
{
    public async Task<IMcpClient> CreateAsync(string jwt, CancellationToken cancellationToken)
    {
        var gatewayMcpUrl = configuration["Gateway:McpUrl"] ?? "http://localhost:5000/mcp";

        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri(gatewayMcpUrl),
            AdditionalHeaders = new Dictionary<string, string>
            {
                ["Authorization"] = $"Bearer {jwt}"
            }
        });

        var client = await McpClient.CreateAsync(transport, cancellationToken: cancellationToken);
        return new McpClientWrapper(client);
    }

    private sealed class McpClientWrapper(McpClient inner) : IMcpClient
    {
        public async Task<IReadOnlyList<McpTool>> ListToolsAsync(CancellationToken cancellationToken)
        {
            var tools = await inner.ListToolsAsync(cancellationToken: cancellationToken);
            return tools
                .Select(t => new McpTool(t.Name, t.Description ?? string.Empty))
                .ToList();
        }

        public async Task<string> CallToolAsync(
            string toolName,
            Dictionary<string, object?> arguments,
            CancellationToken cancellationToken)
        {
            var result = await inner.CallToolAsync(
                toolName, arguments, cancellationToken: cancellationToken);

            return result.Content.OfType<TextContentBlock>().FirstOrDefault()?.Text
                ?? string.Empty;
        }

        public ValueTask DisposeAsync() => inner.DisposeAsync();
    }
}
