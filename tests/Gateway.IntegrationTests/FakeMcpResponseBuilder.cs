namespace Gateway.IntegrationTests;

using System.Text.Json;

public static class FakeMcpResponseBuilder
{
    public static string BuildToolResult(string text, bool isError = false)
    {
        var response = new
        {
            jsonrpc = "2.0",
            id = 1,
            result = new
            {
                content = new[] { new { type = "text", text } },
                isError
            }
        };

        return JsonSerializer.Serialize(response);
    }

    public static string BuildInitializeResult()
    {
        var response = new
        {
            jsonrpc = "2.0",
            id = 1,
            result = new
            {
                protocolVersion = "2024-11-05",
                capabilities = new { },
                serverInfo = new { name = "fake", version = "1.0" }
            }
        };

        return JsonSerializer.Serialize(response);
    }

    public static string BuildToolsList()
    {
        var tools = new[]
        {
            new { name = "list_tasks", description = "Lists tasks", inputSchema = new { type = "object", properties = new { } } },
            new { name = "create_task", description = "Creates task", inputSchema = new { type = "object", properties = new { } } },
            new { name = "complete_task", description = "Completes task", inputSchema = new { type = "object", properties = new { } } },
            new { name = "delete_task", description = "Deletes task", inputSchema = new { type = "object", properties = new { } } }
        };

        var response = new
        {
            jsonrpc = "2.0",
            id = 1,
            result = new { tools }
        };

        return JsonSerializer.Serialize(response);
    }
}
