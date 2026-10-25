namespace Gateway.Core.Contracts
{
    using Gateway.Core.Abstractions;

    public sealed record PagedAuditResult(
        IReadOnlyList<ToolInvocationRecord> Items,
        int TotalCount,
        int Page,
        int PageSize,
        int TotalPages);
}
