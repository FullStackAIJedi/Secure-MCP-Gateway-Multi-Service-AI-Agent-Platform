namespace Gateway.IntegrationTests;

using System.Text.Json;

public sealed class FakeMcpServer
{
    private readonly List<FakeTask> _tasks = new();

    public IReadOnlyList<FakeTask> Tasks => _tasks;

    public string Handle(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return FakeMcpResponseBuilder.BuildToolResult("ok");
        }

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        if (!root.TryGetProperty("method", out var methodProp))
        {
            return FakeMcpResponseBuilder.BuildToolResult("ok");
        }

        var method = methodProp.GetString();

        return method switch
        {
            "initialize" => FakeMcpResponseBuilder.BuildInitializeResult(),
            "tools/list" => FakeMcpResponseBuilder.BuildToolsList(),
            "tools/call" => HandleToolCall(root),
            _ => FakeMcpResponseBuilder.BuildToolResult("ok")
        };
    }

    private string HandleToolCall(JsonElement root)
    {
        var toolName = root.GetProperty("params").GetProperty("name").GetString();
        var args = root.GetProperty("params").TryGetProperty("arguments", out var a) ? a : default;

        return toolName switch
        {
            "list_tasks" => HandleListTasks(args),
            "create_task" => HandleCreateTask(args),
            "complete_task" => HandleCompleteTask(args),
            "delete_task" => HandleDeleteTask(args),
            _ => FakeMcpResponseBuilder.BuildToolResult("unknown tool", isError: true)
        };
    }

    private string HandleListTasks(JsonElement args)
    {
        var subjectId = args.TryGetProperty("subjectId", out var s) ? s.GetString() : null;
        var filtered = _tasks.Where(t => t.OwnerSubjectId == subjectId).ToList();
        return FakeMcpResponseBuilder.BuildToolResult(JsonSerializer.Serialize(filtered));
    }

    private string HandleCreateTask(JsonElement args)
    {
        var task = new FakeTask
        {
            Id = _tasks.Count + 1,
            Title = args.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "",
            Description = args.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "",
            OwnerSubjectId = args.TryGetProperty("subjectId", out var s) ? s.GetString() ?? "" : "",
            IsComplete = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _tasks.Add(task);
        return FakeMcpResponseBuilder.BuildToolResult(JsonSerializer.Serialize(task));
    }

    private string HandleCompleteTask(JsonElement args)
    {
        var subjectId = args.TryGetProperty("subjectId", out var s) ? s.GetString() : null;
        var id = args.TryGetProperty("id", out var i) ? i.GetInt32() : 0;

        var task = _tasks.FirstOrDefault(t => t.Id == id && t.OwnerSubjectId == subjectId);
        if (task is null)
        {
            return FakeMcpResponseBuilder.BuildToolResult("false");
        }

        task.IsComplete = true;
        return FakeMcpResponseBuilder.BuildToolResult("true");
    }

    private string HandleDeleteTask(JsonElement args)
    {
        var subjectId = args.TryGetProperty("subjectId", out var s) ? s.GetString() : null;
        var id = args.TryGetProperty("id", out var i) ? i.GetInt32() : 0;

        var task = _tasks.FirstOrDefault(t => t.Id == id && t.OwnerSubjectId == subjectId);
        if (task is null)
        {
            return FakeMcpResponseBuilder.BuildToolResult("false");
        }

        _tasks.Remove(task);
        return FakeMcpResponseBuilder.BuildToolResult("true");
    }
}
