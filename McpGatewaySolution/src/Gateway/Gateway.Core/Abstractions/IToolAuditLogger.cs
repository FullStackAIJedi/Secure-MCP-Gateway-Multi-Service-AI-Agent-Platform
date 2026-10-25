namespace Gateway.Core.Abstractions
{
    using Gateway.Core.Contracts;
    public interface IToolAuditLogger
    {
        Task LogAsync(ToolInvocationRecord record, CancellationToken cancellationToken);
    } 
}
