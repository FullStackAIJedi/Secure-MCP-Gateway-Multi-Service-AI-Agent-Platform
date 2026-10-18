namespace Gateway.Core.Abstractions
{  
    public interface IToolAuditLogger
    {
        Task LogAsync(ToolInvocationRecord record, CancellationToken cancellationToken);
    }

    public sealed record ToolInvocationRecord(
        string SubjectId,
        string ToolName,
        string DownstreamService,
        string Outcome,
        DateTimeOffset Timestamp,
        long DurationMs);
}
