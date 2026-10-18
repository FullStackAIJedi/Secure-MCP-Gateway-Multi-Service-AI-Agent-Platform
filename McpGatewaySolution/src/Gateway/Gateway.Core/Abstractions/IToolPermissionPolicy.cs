namespace Gateway.Core.Abstractions
{ 
    public interface IToolPermissionPolicy
    {
        Task<bool> IsAllowedAsync(
            string subjectId,
            string toolName,
            string downstreamService,
            CancellationToken cancellationToken);
    }
}
