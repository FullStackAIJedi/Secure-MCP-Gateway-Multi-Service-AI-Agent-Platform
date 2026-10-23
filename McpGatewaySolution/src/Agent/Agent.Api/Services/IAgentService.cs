namespace Agent.Api.Services
{  
    public interface IAgentService
    {
         Task<(string Reply, List<string> ToolsUsed)> ChatAsync(
            string sessionId, string message, string jwt, CancellationToken cancellationToken);
        Task<string> ExecuteApprovedToolAsync(
            string toolName, string arguments, string jwt, CancellationToken cancellationToken);
    }
}
