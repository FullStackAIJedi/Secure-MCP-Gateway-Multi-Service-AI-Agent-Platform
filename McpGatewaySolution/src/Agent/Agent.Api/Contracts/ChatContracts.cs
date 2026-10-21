namespace Agent.Api.Contracts
{
    //public sealed record ChatRequest(string SessionId, string Message); 
    //public sealed record ChatResponse(string Reply);
    public sealed record ChatRequest(string SessionId, string Message); 
    public sealed record ChatResponse(string Reply, List<string> ToolsUsed); 
    public sealed record ToolUsage(string ToolName, bool Success);
}
