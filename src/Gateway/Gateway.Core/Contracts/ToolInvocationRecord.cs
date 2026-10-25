namespace Gateway.Core.Contracts
{
    public sealed record ToolInvocationRecord(
        string SubjectId,
        string ToolName,
        string DownstreamService,
        string Outcome,
        DateTimeOffset Timestamp,
        long DurationMs,
        string? TraceId = null,
        string? SpanId = null,
        string? ServiceName = null,
        string? Environment = null,
        string? UserEmail = null,
        string? ToolScope = null,
        string? ErrorMessage = null);
}
