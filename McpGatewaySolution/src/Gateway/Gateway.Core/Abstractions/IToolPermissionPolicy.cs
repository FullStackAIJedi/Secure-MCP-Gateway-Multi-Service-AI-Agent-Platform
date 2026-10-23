//namespace Gateway.Core.Abstractions
//{ 
//    public interface IToolPermissionPolicy
//    {
//        Task<bool> IsAllowedAsync(
//            string subjectId,
//            string toolName,
//            string downstreamService,
//            CancellationToken cancellationToken);
//    }
//}

namespace Gateway.Core.Abstractions
{
    using Gateway.Core.Scopes;

    public interface IToolPermissionPolicy
    {
        Task<bool> IsAllowedAsync(
            string subjectId,
            string toolName,
            IReadOnlySet<ToolScope> userScopes,
            CancellationToken cancellationToken);
    }
}