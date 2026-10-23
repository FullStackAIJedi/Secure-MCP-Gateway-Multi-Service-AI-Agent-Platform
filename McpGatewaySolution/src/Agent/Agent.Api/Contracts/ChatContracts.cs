namespace Agent.Api.Contracts
{ 
    public sealed record ChatRequest(string SessionId, string Message); 
    public sealed record ChatResponse(string Reply, List<string> ToolsUsed); 
    public sealed record ToolUsage(string ToolName, bool Success);
    public sealed record ApprovalRequest(string ToolName, string Arguments);
    public sealed record ApprovalResponse(string Result);
}
