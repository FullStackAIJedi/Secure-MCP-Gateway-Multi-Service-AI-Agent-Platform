namespace Gateway.Core.Policies
{
    using Gateway.Core.Abstractions;
    using Gateway.Core.Scopes;

    public sealed class ScopeBasedToolPermissionPolicy(IToolRegistry registry) : IToolPermissionPolicy
    {
        public Task<bool> IsAllowedAsync(
            string subjectId,
            string toolName,
            IReadOnlySet<ToolScope> userScopes,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(subjectId))
                return Task.FromResult(false);

            if (string.IsNullOrWhiteSpace(toolName))
                return Task.FromResult(false);

            if (!registry.TryGetScope(toolName, out var requiredScope))
                return Task.FromResult(false);

            var allowed = requiredScope switch
            {
                ToolScope.Read => userScopes.Contains(ToolScope.Read),
                ToolScope.Write => userScopes.Contains(ToolScope.Write),
                ToolScope.Admin => userScopes.Contains(ToolScope.Admin),
                _ => false
            };

            return Task.FromResult(allowed);
        }
    }
}